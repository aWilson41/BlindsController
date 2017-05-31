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

        // Called when program starts
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            SetupPWM().Wait();

            while (true)
            {
                // Wait 100s
                Task.Delay(60000).Wait();

                // Test what percetange it should be at given the time
                int hr = DateTime.Now.Hour;
                int min = DateTime.Now.Minute;
                // If hour 20 (8pm) and past 30 mins (8:30pm) or if past hour is over 20 (8pm) or if hour is under 7
                if ((hr == 20 && min >= 30) || hr > 20 || hr < 7)
                    currPercent = 0.025; // Blinds down
                else
                    currPercent = 0.075; // Blinds up

                // If the percentage has changed from the last update
                if (currPercent != prevPercent)
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

        private async Task SetupPWM()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
                var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                var pwmController = pwmControllers[1]; // use the on-device controller
                pwmController.SetDesiredFrequency(50); // try to match 50Hz
                pin = pwmController.OpenPin(21);
                //pin.SetActiveDutyCyclePercentage(currPercent);
                //pin.Start();
            }
        }
    }
}
