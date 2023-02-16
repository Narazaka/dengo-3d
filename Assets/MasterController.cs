using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Device.Net;
using Usb.Net.Windows;
using Cysharp.Threading.Tasks;

public class MasterController : MonoBehaviour
{
    static int AccelId = Animator.StringToHash("Accel");
    static int BrakeId = Animator.StringToHash("Brake");

    [SerializeField]
    Button GetDeviceButton;
    [SerializeField]
    Animator Animator;

    IDevice Device { get; set; }
    DengoValue ControllerValue = new DengoValue();
    class DengoValue
    {
        static Dictionary<byte, int> Brakes = new Dictionary<byte, int> {
            { 0x79, 0 },
            { 0x8A, 1 },
            { 0x94, 2 },
            { 0x9A, 3 },
            { 0xA2, 4 },
            { 0xA8, 5 },
            { 0xAF, 6 },
            { 0xB2, 7 },
            { 0xB5, 8 },
            { 0xB9, 9 },
        };
        static Dictionary<byte, int> Accels = new Dictionary<byte, int> {
            { 0x81, 0 },
            { 0x6D, 1 },
            { 0x54, 2 },
            { 0x3F, 3 },
            { 0x21, 4 },
            { 0x00, 5 },
        };
        static float MaxBrake = Brakes.Values.Max();
        static float MaxAccel = Accels.Values.Max();
        const byte InvalidValue = 0xFF;

        public void SetValue(byte[] values)
        {
            var brakeValue = values[1];
            if (brakeValue != InvalidValue && brakeValue != 0)
            {
                Brake = Brakes[brakeValue];
            }
            var accelsValue = values[2];
            if (accelsValue != InvalidValue)
            {
                Accel = Accels[accelsValue];
            }
        }
        public int Brake;
        public int Accel;
        public float BrakeRate { get => Brake / MaxBrake; }
        public float AccelRate { get => Accel / MaxAccel; }
    }

    async UniTask Start()
    {
        await GetDevice();
        GetDeviceButton.onClick.AddListener(async () => await GetDevice());
    }

    async void Update()
    {
        await LoadStatus();
        Animator.SetFloat(AccelId, ControllerValue.AccelRate);
        Animator.SetFloat(BrakeId, ControllerValue.BrakeRate);
    }

    async UniTask GetDevice()
    {
        var filter = new FilterDeviceDefinition(vendorId: 0x0ae4, productId: 0x0004);
        var deviceFactory = filter.CreateWindowsUsbDeviceFactory();
        var defs = (await deviceFactory.GetConnectedDeviceDefinitionsAsync().AsUniTask()).ToList();
        if (defs.Count == 0) return;
        var device = await deviceFactory.GetDeviceAsync(defs.First()).AsUniTask();
        await device.InitializeAsync().AsUniTask();
        Device = device;
    }

    async UniTask LoadStatus()
    {
        if (Device == null) return;
        try
        {
            var res = await Device.ReadAsync().AsUniTask();
            ControllerValue.SetValue(res.Data);
        }
        catch (Device.Net.Exceptions.ApiException)
        {
            Device = null;
        }
    }
}
