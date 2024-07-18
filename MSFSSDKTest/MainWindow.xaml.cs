using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace MSFSSDKTest;

public class SerialVar<T>(T value) where T : notnull {
	T _Value = value;

	public T RawValue {
		set {
			_Value = value;
		}
	}

	public T Value {
		get => _Value;

		set {
			if (!value.Equals(_Value))
				Changed = true;

			_Value = value;
		}
	}

	public bool Changed = false;
}

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();

		// SimConnect
		Sim = new(this);

		// Serial
		Serial.DataReceived += OnSerialDataReceived;

		Sim.Start();
		RotationStopwatch.Start();
		StartSerial();
	}

	Sim Sim;
	Stopwatch RotationStopwatch = new();

	SerialPort Serial = new("COM6", 115200, Parity.None, 8, StopBits.One);

	public ThrottleDisplayMode ThrottleMode = DisplayMode.Register(new ThrottleDisplayMode());
	public MixtureDisplayMode MixtureMode = DisplayMode.Register(new MixtureDisplayMode());

	public ElevatorTrimDisplayMode ElevatorTrimMode = DisplayMode.Register(new ElevatorTrimDisplayMode());

	public ParkingBreaksDisplayMode ParkingBreakMode = DisplayMode.Register(new ParkingBreaksDisplayMode());

	public AutopilotDisplayMode APMode = DisplayMode.Register(new AutopilotDisplayMode());
	public APFLCDisplayMode APFLCMode = DisplayMode.Register(new APFLCDisplayMode());
	public APHDGHoldDisplayMode APHDGHoldMode = DisplayMode.Register(new APHDGHoldDisplayMode());
	public APVSHoldDisplayMode APVSHoldMode = DisplayMode.Register(new APVSHoldDisplayMode());

	public SpeedDisplayMode APSPDMode = DisplayMode.Register(new SpeedDisplayMode());
	public HeadingDisplayMode APHDGMode = DisplayMode.Register(new HeadingDisplayMode());
	public AltitudeDisplayMode APALTMode = DisplayMode.Register(new AltitudeDisplayMode());
	public VerticalSpeedDisplayMode APVSMode = DisplayMode.Register(new VerticalSpeedDisplayMode());
	
	public SerialVar<int> ModeIndex = new(0);
	public SerialVar<bool> ModeMode = new(false);

	// ------------------------------- Serial -------------------------------

	public void SerialWriteFull() {
		var mode = DisplayMode.Instances[ModeIndex.Value];

		byte[] buffer = [
			(byte) SerialCommand.ModeIndex,
			(byte) ModeIndex.Value,

			(byte) SerialCommand.ModeCount,
			(byte) DisplayMode.Instances.Count,

			(byte) SerialCommand.ModeMode,
			(byte) (ModeMode.Value ? 1 : 0),

			(byte) SerialCommand.Title,
			..Encoding.ASCII.GetBytes(mode.Title.Value),
			(byte) '\n',

			(byte) SerialCommand.Body,
			..Encoding.ASCII.GetBytes(mode.BodyToString()),
			(byte) '\n',

			(byte) SerialCommand.Suffix,
			..Encoding.ASCII.GetBytes(mode.Suffix.Value),
			(byte) '\n',
		];

		Serial.Write(buffer, 0, buffer.Length);
	}

	public void SerialWriteModeIndex() {
		byte[] buffer = [
			(byte) SerialCommand.ModeIndex,
			(byte) ModeIndex.Value
		];

		Serial.Write(buffer, 0, buffer.Length);
	}

	public void SerialWriteModeMode() {
		byte[] buffer = [
			(byte) SerialCommand.ModeMode,
			(byte) (ModeMode.Value ? 1 : 0)
		];

		Serial.Write(buffer, 0, buffer.Length);
	}

	public void SerialWriteModeValueChecked() {
		var mode = DisplayMode.Instances[ModeIndex.Value];

		if (mode.Body.Changed) {
			byte[] buffer = [
				(byte) SerialCommand.Body,
				..Encoding.ASCII.GetBytes(mode.BodyToString()),
				(byte) '\n',
			];

			Serial.Write(buffer, 0, buffer.Length);

			mode.Body.Changed = false;
		}

		if (mode.Suffix.Changed) {
			byte[] buffer = [
				(byte) SerialCommand.Suffix,
				..Encoding.ASCII.GetBytes(mode.Suffix.Value),
				(byte) '\n',
			];

			Serial.Write(buffer, 0, buffer.Length);

			mode.Body.Changed = false;
		}
	}

	public void OnSerialDataReceived(object s, SerialDataReceivedEventArgs e) {
		try {
			while (Serial.BytesToRead > 0) {
				switch ((SerialCommand) Serial.ReadByte()) {
					case SerialCommand.Reset:
						SerialWriteFull();
						break;

					case SerialCommand.Rotation:
						var right = Serial.ReadByte() > 0;

						// Mode change
						if (ModeMode.Value) {
							ModeIndex.Value = Math.Clamp(ModeIndex.Value + (right ? 1 : -1), 0, DisplayMode.Instances.Count - 1);
							SerialWriteFull();
						}
						// Rotation
						else {
							var mode = DisplayMode.Instances[ModeIndex.Value];

							mode.FromRotation(RotationStopwatch.ElapsedMilliseconds, right);
							mode.SendSimEvent(Sim);

							RotationStopwatch.Restart();
						}

						break;

					case SerialCommand.Pressed:
						var pressed = Serial.ReadByte() == 1;

						if (pressed) {
							ModeMode.Value = !ModeMode.Value;
							SerialWriteModeMode();
						}

						break;

				}
			}
		}
		catch (Exception ex) {

		}
 	}

	void StartSerial() {
		try {
			Serial.Open();

			SerialWriteFull();
		}
		catch (Exception ex) {
			StopSerial();
		}
	}

	void StopSerial() {
	}
}