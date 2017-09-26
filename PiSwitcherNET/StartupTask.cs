using System;
using System.Threading.Tasks;
using Microsoft.IoT.Lightning.Providers;
using Windows.ApplicationModel.Background;
using Windows.Devices.Pwm;
using Windows.Devices;

namespace PiSwitcherNET
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        PwmPin pin;
        double currPercent = 0.01;
        double prevPercent = 0.01;
		bool flip = false;

        // Called when program starts
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

			// Provide callback for messages
			StaticListener.MessageReceived += ConnectionReceived;

            SetupPWM().Wait();

            while (true)
            {
                // Wait 60s
                Task.Delay(60000).Wait();

				currPercent = DetermineActiveDutyCycle();

                // If the percentage has changed from the last update
                if (currPercent != prevPercent)
					RotateServo();
            }
        }

		// Interpret client messages
		private async void ConnectionReceived(object sender, SocketEventArgs e)
		{
			if (e.message == "A7xrev02")
			{
				flip = !flip;
				currPercent = DetermineActiveDutyCycle();
				RotateServo();
			}
		}

		private async Task SetupPWM()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
                var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                var pwmController = pwmControllers[1]; // use the on-device controller
                pwmController.SetDesiredFrequency(50); // try to match 50Hz
                pin = pwmController.OpenPin(21);
            }
        }

		public double DetermineActiveDutyCycle()
		{
			// Test what percetange it should be at given the time
			int hr = DateTime.Now.Hour;
			int min = DateTime.Now.Minute;
			if (!flip)
			{
				// If hour is over 20 (8pm) or if hour is under 10pm
				if (hr >= 20 || hr < 10)
					return 0.025; // Blinds down
				else
					return 0.075; // Blinds up
			}
			else
			{
				// If hour 20 (8pm) and past 30 mins (8:30pm) or if past hour is over 20 (8pm) or if hour is under 10
				if (hr >= 20 || hr < 10)
					return 0.075; // Blinds up
				else
					return 0.025; // Blinds down
			}
		}

		public void RotateServo()
		{
			// Start the servo with pwm
			pin.SetActiveDutyCyclePercentage(currPercent);
			pin.Start();

			// Update the previous duty cycle
			prevPercent = currPercent;
			// Wait 1s for servo to turn
			Task.Delay(1000).Wait();
			// Stop the servo
			pin.Stop();
		}
    }
}