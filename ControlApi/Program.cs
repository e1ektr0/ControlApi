using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace ControlApi
{
    enum Commands
    {
        MouseMove = 0,//commad,deltax,delatay
        CtrScroll = 1,//commad,rotatecount
        CtrMouseMouse = 2,//commad,deltax,delatay
        ShiftMosue = 3,//commad,deltax,delatay
        Scroll = 4,//commad,rotatecount
        NumpadPress = 5,//command,padnumber
        NumpadDown = 6,//command,padnumber
        NumpadUp = 7,//command,padnumber
        MiddleDown = 8,//command
        MiddleUp = 9,//command
        CtrDown = 10,//command
        CtrUp = 11//command
    }

    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en");
            var allDevices = UsbDevice.AllDevices;
            foreach (UsbRegistry usbRegistry in allDevices)
            {
                if (usbRegistry.DeviceProperties["Class"].ToString() != "AndroidUsbDeviceClass")
                    continue;

                var device = UsbDevice.OpenUsbDevice(n => usbRegistry.Pid == n.Pid);
                if (device == null)
                    throw new Exception("Device Not Found.");
                Console.WriteLine(device.Info.ProductString + " - Connected");
                var wholeUsbDevice = device as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                }
                var reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                var stream = new MemoryStream();
                new Task(() => ReadFromClient(stream)).Start();
                reader.DataReceived += (u, e) =>
                {
                    var p = stream.Position;
                    stream.Write(e.Buffer, 0, e.Count);
                    stream.Flush();
                    stream.Seek(p, SeekOrigin.Begin);
                };

            }

            foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
                foreach (UnicastIPAddressInformation ua in i.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    try
                    {
                        var port = 4520;
                        TcpListener listener = new TcpListener(ua.Address, port);
                        listener.Start();
                        Console.WriteLine("start on:" + listener.LocalEndpoint);
                        new Task(() =>
                        {
                            while (true)
                            {

                                var client = listener.AcceptTcpClient();
                                new Task(() => AcceptConnection(client)).Start();
                            }
                        }).Start();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("fail for:" + ua.Address);
                    }
                }

            Console.WriteLine("Server listening on port 4520. Press enter to exit.");
            Console.ReadLine();
        }

        private static void AcceptConnection(TcpClient client)
        {
            Console.WriteLine("Clien connection");
            try
            {
                var clientStream = client.GetStream();
                new Task(() => ReadFromClient(clientStream)).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        private static void ReadFromClient(Stream clientStream)
        {
            var inputSimulator = new InputSimulator();
            var reader = new StreamReader(clientStream);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;
                try
                {
                    var param = line.Split(',').Select(n => n.Trim()).ToArray();
                    var commandId = (Commands)int.Parse(param[0]);
                    switch (commandId)
                    {
                        case Commands.MouseMove:
                            MouseMove(param, inputSimulator);
                            break;
                            
                        case Commands.CtrScroll:
                            inputSimulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                            inputSimulator.Mouse.VerticalScroll(int.Parse(param[1]));
                            inputSimulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                            break;
                        case Commands.CtrMouseMouse:
                            MouseMoveModify(param, inputSimulator, VirtualKeyCode.CONTROL);
                            break;
                        case Commands.ShiftMosue:
                            MouseMoveModify(param, inputSimulator, VirtualKeyCode.SHIFT);
                            break;
                        case Commands.Scroll:
                            inputSimulator.Mouse.VerticalScroll(int.Parse(param[1]));
                            break;
                        case Commands.NumpadPress:
                            {
                                var pad = int.Parse(param[1]);
                                var x = (VirtualKeyCode)(pad + (int)VirtualKeyCode.NUMPAD0);
                                inputSimulator.Keyboard.KeyPress(x);
                            }
                            break;
                        case Commands.NumpadDown:
                            {
                                var pad = int.Parse(param[1]);
                                var x = (VirtualKeyCode)(pad + (int)VirtualKeyCode.NUMPAD0);
                                inputSimulator.Keyboard.KeyDown(x);
                            }
                            break;
                        case Commands.NumpadUp:
                            {
                                var pad = int.Parse(param[1]);
                                var x = (VirtualKeyCode)(pad + (int)VirtualKeyCode.NUMPAD0);
                                inputSimulator.Keyboard.KeyUp(x);
                            }
                            break;
                        case Commands.MiddleDown:
                            inputSimulator.Mouse.MiddleButtonDown();
                            break;
                        case Commands.MiddleUp:
                            inputSimulator.Mouse.MiddleButtonUp();
                            break;
                        case Commands.CtrDown:
                            inputSimulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                            break;
                        case Commands.CtrUp:
                            inputSimulator.Keyboard.KeyUp(VirtualKeyCode.UP);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    Console.WriteLine("command:" + line);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + ":" + line);
                }
            }
        }
        private static void MouseMoveModify(string[] param, InputSimulator inputSimulator, VirtualKeyCode key)
        {
            var deltaX = int.Parse(param[1]);
            var deltaY = int.Parse(param[2]);
            inputSimulator.Keyboard.KeyDown(key);
            inputSimulator.Mouse.MoveMouseBy(deltaX, deltaY);
            inputSimulator.Keyboard.KeyUp(key);
        }

        private static void MouseMove(string[] param, InputSimulator inputSimulator)
        {
            var deltaX = int.Parse(param[1]);
            var deltaY = int.Parse(param[2]);
            inputSimulator.Mouse.MoveMouseBy(deltaX, deltaY);
        }
    }
}
