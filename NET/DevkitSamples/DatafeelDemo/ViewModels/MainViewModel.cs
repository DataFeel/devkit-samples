using Datafeel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Diagnostics;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Disposables;
using AsyncAwaitBestPractices;
using DynamicData.Binding;
using Datafeel.NET.BLE;
using Datafeel.NET.Serial;
using Serilog;
using Reflection = Datafeel.Reflection;
using System.Threading;

namespace DatafeelDemo
{
    public class MainViewModel : ReactiveObject, IDotProps, IActivatableViewModel
    {
        private List<IDotProps> _dotStore = new List<IDotProps>()
        {
            new DotProps(1),
            new DotProps(2),
            new DotProps(3),
            new DotProps(4),
        };

        private SemaphoreSlim PropertySheetSemaphore = new SemaphoreSlim(1, 1);

        #region Properties
        //[Reactive] public string? ShortSerial => SerialNumber?.Substring(0, 4);

        [Reactive] public byte Address { get; set; }
        [Reactive] public bool? IsConnected { get; set; }
        [Reactive] public string? DeviceName { get; set; }
        [Reactive] public string? HardwareID { get; set; }
        [Reactive] public string? FirmwareID { get; set; }
        [Reactive] public string? SerialNumber { get; set; }
        [Reactive] public string? DeviceLabel { get; set; }
        [Reactive] public float? AccelerationX { get; set; }
        [Reactive] public float? AccelerationY { get; set; }
        [Reactive] public float? AccelerationZ { get; set; }
        [Reactive] public float? GyroscopeX { get; set; }
        [Reactive] public float? GyroscopeY { get; set; }
        [Reactive] public float? GyroscopeZ { get; set; }
        [Reactive] public ThermalModes? ThermalMode { get; set; } = ThermalModes.Off;
        [Reactive] public float? ThermalIntensity { get; set; }
        [Reactive] public float? TargetSkinTemperature { get; set; }
        [Reactive] public float? SkinTemperature { get; set; }
        [Reactive] public float? HeatsinkTemperature { get; set; }
        [Reactive] public LedModes? LedMode { get; set; } = LedModes.Breathe;
        [Reactive] public RgbLed? GlobalLed { get; set; }
        [Reactive] public RgbLed[]? IndividualManualLeds { get; set; }
        [Reactive] public VibrationModes? VibrationMode { get; set; } = VibrationModes.Off;
        [Reactive] public VibrationSegment[]? VibrationSequence { get; set; }
        [Reactive] public float? VibrationFrequency { get; set; }
        [Reactive] public float? VibrationIntensity { get; set; }
        [Reactive] public bool? VibrationGo { get; set; }
        // RGB Slider Props
        [Reactive] public byte Red { get; set; } = 0;
        [Reactive] public byte Green { get; set; } = 0;
        [Reactive] public byte Blue { get; set; } = 0;

        #endregion
        public ViewModelActivator Activator { get; } = new();
        private IDotService _dotService;
        public List<VibrationModes?> VibrationModesList => Enum.GetValues(typeof(VibrationModes)).Cast<VibrationModes?>().ToList();
        public List<LedModes?> LedModesList => Enum.GetValues(typeof(LedModes)).Cast<LedModes?>().ToList();
        public List<ThermalModes?> ThermalModesList => Enum.GetValues(typeof(ThermalModes)).Cast<ThermalModes?>().ToList();
        public List<byte> DotAddressList => Enumerable.Range(1, 4).Select(x => (byte)x).ToList();


        [Reactive] public bool IsManagerConnected { get; set; } = false;

        public MainViewModel()
        {
            _dotService = ServiceHelper.GetService<IDotService>() ?? throw new InvalidOperationException("DotService not found");
            var manager = _dotService.DotManager;
            Address = 1;

            // Keep a collection of IDotProps objects as a source of truth
            // When the address is changed, clone the corresponding props into the UI props
            // When the UI props are changed, clone them into the corresponding props object before fire off a write command using that object
            // Only clone read results into the original collection 

            // Alternatively, or maybe in addition to that, we can abort all pending commands every time the address is changed to prevent overwriting the wrong state object

            this.WhenActivated(async (disposables) =>
            {
                //DebugMessage("Verifying Bluetooth permissions..");
                var permissionResult = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
                if (permissionResult != PermissionStatus.Granted)
                {
                    permissionResult = await Permissions.RequestAsync<Permissions.Bluetooth>();
                }
                //DebugMessage($"Result of requesting Bluetooth permissions: '{permissionResult}'");
                if (permissionResult != PermissionStatus.Granted)
                {
                    //DebugMessage("Permissions not available, direct user to settings screen.");
                    //ShowMessage("Permission denied. Not scanning.");
                    AppInfo.ShowSettingsUI();
                    //return false;
                }

                var cancelRetry = new CancellationTokenSource();
                var retry = Task.Run(async () =>
                {
                    while (cancelRetry.Token.IsCancellationRequested == false)
                    {
                        try
                        {
                            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                            var serialClient = new DatafeelModbusClientConfiguration()
                                .UseWindowsSerialPortTransceiver()
                                .CreateClient();
                            var bleClient = new DatafeelModbusClientConfiguration()
                                .UseNetBleTransceiver()
                                .CreateClient();
                            var clients = new List<DatafeelModbusClient> { serialClient, bleClient };
                            await manager.Start(clients, cts.Token);
                            IsManagerConnected = true;

                            await _dotService.DotManager.Runner;
                            IsManagerConnected = false;
                        }
                        catch (Exception e)
                        {
                            //Debug.WriteLine(e.Message);
                        }

                        await Task.Delay(1000);
                    }
                }, cancelRetry.Token);

                this.WhenAnyValue(x => x.Address)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(async (address) =>
                {
                    if (manager.IsRunning is false) return;
                    Debug.WriteLine("Address Changed");
                    // Load state from store
                    await PropertySheetSemaphore.WaitAsync();
                    var props = SearchDotStore(address);
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        props.CopyInto(this);
                        Red = props.GlobalLed.Red;
                        Green = props.GlobalLed.Green;
                        Blue = props.GlobalLed.Blue;
                    });
                    PropertySheetSemaphore.Release();
                }).DisposeWith(disposables);

                //var writableProps = typeof(DotPropsWritable).GetProperties().Select(x => x.Name).ToArray();
                var writableProps = Reflection.GetWritableProperties(typeof(DotPropsWritable));
                // TODO: use color picker instead of sliders
                writableProps.Remove(nameof(Address)); // will be handled from a separate listenter
                writableProps.Add(nameof(Red));
                writableProps.Add(nameof(Green));
                writableProps.Add(nameof(Blue));
                writableProps.Remove(nameof(VibrationGo));
                writableProps.Remove(nameof(DeviceLabel));
                writableProps.Remove(nameof(GlobalLed));
                //var writableProps = new string[] { "Red" };

                this.WhenAnyPropertyChanged(writableProps.ToArray())
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(async (_) =>
                {
                    if (manager.IsRunning is false) return;
                    Debug.WriteLine("Writing");
                    // TODO: Check if the Address has changed, if so, await abort any queued operations so our address doesn't get overwritten
                    // TODO: Alternatively, edit our custom Reflection helpers to never copy the Address property. (kind of a hack)
                    //Debug.WriteLine($"Property was changed");
                    //GlobalLed = new RgbLed() { Red = Red, Green = Green, Blue = Blue };
                    if (_dotService.DotManager.IsRunning == false) return;

                    // Update the store with new values and settings
                    await PropertySheetSemaphore.WaitAsync();
                    var currentDot = SearchDotStore(Address);
                    currentDot.GlobalLed.Red = Red;
                    currentDot.GlobalLed.Green = Green;
                    currentDot.GlobalLed.Blue = Blue;
                    this.CopyInto(currentDot);
                    PropertySheetSemaphore.Release();

                    // Write the freshly stored state to the hardware
                    try
                    {
                        await _dotService.DotManager.Write(currentDot);
                    }
                    catch (Exception e)
                    {
                        //Debug.WriteLine(e.Message);
                    }
                    
                })
                .DisposeWith(disposables);

                Observable.Interval(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.TaskpoolScheduler)
                    .Subscribe(async (_) =>
                    {
                        if (manager.IsRunning is false) return;
                        //Debug.WriteLine("Reading");
                        if (_dotService.DotManager.IsRunning == false)
                        {
                            // grey out all UI and then return

                        }
                        await PropertySheetSemaphore.WaitAsync();
                        var currentDot = SearchDotStore(Address);
                        try
                        {
                            using (var cts = new CancellationTokenSource(50))
                            {
                                // Read from the hardware and update our store
                                var props = await manager.Read(currentDot, cts.Token);
                                props.CopyInto(currentDot);
                            }
                        }
                        catch (Exception e)
                        {
                            //Debug.WriteLine(e.Message);
                        }

                        // Update our UI using the values from our store
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            currentDot.CopyInto(this);
                            
                            this.HeatsinkTemperature = currentDot.HeatsinkTemperature;
                            this.SkinTemperature = currentDot.SkinTemperature;
                            Debug.WriteLine(this.HeatsinkTemperature);
                            Debug.WriteLine(this.SkinTemperature);
                        });
                        PropertySheetSemaphore.Release();
                    })
                    .DisposeWith(disposables);
            });
        }

        private IDotProps SearchDotStore(byte address)
        {
            return _dotStore.FirstOrDefault(x => x.Address == address) ?? new DotProps(address);
        }


        //private void ReplaceDotProps(IDotProps props)
        //{
        //    var index = _dotProps.FindIndex(x => x.Address == props.Address);
        //    if (index >= 0)
        //    {
        //        _dotProps[index] = props;
        //    }
        //    else
        //    {
        //        _dotProps.Add(props);
        //    }
        //}

        public void CopyInto(object other)
        {
            Datafeel.Reflection.CopyProperties(this, other);
        }
        public void CopyFrom(object other)
        {
            Datafeel.Reflection.CopyProperties(other, this);
        }
    }
}
