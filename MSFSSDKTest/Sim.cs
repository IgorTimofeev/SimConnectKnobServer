using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MSFSSDKTest;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SimData {
	public double Throttle;
	public double Mixture;
	public double Speed;
	public double Heading;
	public double Altitude;
	public double VerticalSpeed;
	public double Trim;
}

public enum SimDataRequest {
	Request1
}

public enum SimDefinition {
	SimData
}

public enum SimEvent {
	THROTTLE_SET,
	MIXTURE_SET,
	AP_SPD_VAR_SET,
	HEADING_BUG_SET,
	AP_ALT_VAR_SET_ENGLISH,
	AP_VS_VAR_SET_ENGLISH,
	ELEVATOR_TRIM_SET
}

public enum SimNotificationGroup {
	Group0
}

public enum SerialCommand : byte {
	Rotation,
	Pressed,

	Reset,
	ModeIndex,
	ModeCount,
	ModeMode,
	Title,
	Suffix,
	Body
};

public class Sim {
	public Sim(MainWindow mainWindow) {
		MainWindow = mainWindow;

		SimConnectTimer = new(
			TimeSpan.FromMilliseconds(10),
			DispatcherPriority.ApplicationIdle,
			OnSimConnectTimerTick,
			MainWindow.Dispatcher
		);

		SimConnectTimer.Stop();
	}

	MainWindow MainWindow;
	DispatcherTimer SimConnectTimer;
	SimConnect? SimConnect = null;

	void OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {

	}

	void OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {

	}

	void OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data) {
		Stop();
	}

	void OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data) {
		if (data.dwRequestID == 0) {
			var simData = (SimData) data.dwData[0];

			MainWindow.ThrottleMode.Body.Value = (int) Math.Round(simData.Throttle);
			MainWindow.MixtureMode.Body.Value = (int) Math.Round(simData.Mixture);
			MainWindow.SpeedMode.Body.Value = (int) Math.Round(simData.Speed);
			MainWindow.HeadingMode.Body.Value = (int) Math.Round(simData.Heading);
			MainWindow.AltitudeMode.Body.Value = (int) Math.Round(simData.Altitude);
			MainWindow.VerticalSpeedMode.Body.Value = (int) Math.Round(simData.VerticalSpeed * 60);
			MainWindow.TrimMode.Body.Value = (int) Math.Round(simData.Trim);

			MainWindow.SerialWriteModeValueChecked();

			MainWindow.AltitudeTextBox.Clear();

			foreach (var mode in DisplayMode.Instances)
				MainWindow.AltitudeTextBox.AppendText($"{mode.Title.Value}: {mode.Body.Value} {mode.Suffix.Value}\n");
		}
		else {
			//label_status.Text = "Unknown request ID: " + ((uint) data.dwRequestID);
		}
	}

	void OnSimConnectTimerTick(object? s, EventArgs e) {
		SimConnect!.ReceiveMessage();
		SimConnect!.RequestDataOnSimObjectType(SimDataRequest.Request1, SimDefinition.SimData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
	}

	public void Start() {
		try {
			SimConnect = new(
				"Managed Data Request",
				new WindowInteropHelper(MainWindow).Handle,
				0x0402,
				null,
				0
			);

			SimConnect.OnRecvOpen += new(OnRecvOpen);
			SimConnect.OnRecvQuit += new(OnRecvQuit);
			SimConnect.OnRecvException += new(OnRecvException);
			SimConnect.OnRecvSimobjectDataBytype += new(OnRecvSimobjectDataBytype);

			SimConnect.AddToDataDefinition(SimDefinition.SimData, "GENERAL ENG THROTTLE LEVER POSITION:1", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "GENERAL ENG MIXTURE LEVER POSITION:1", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT AIRSPEED HOLD VAR", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT HEADING LOCK DIR", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT ALTITUDE LOCK VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT VERTICAL HOLD VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "ELEVATOR TRIM POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

			SimConnect.RegisterDataDefineStruct<SimData>(SimDefinition.SimData);

			SimConnectTimer.Start();
		}
		catch (COMException ex) {
			Stop();
		}
	}

	void Stop() {
		if (SimConnect == null)
			return;

		SimConnect.Dispose();
		SimConnect = null;

		SimConnectTimer.Stop();
	}

	public void TransmitEvent(Enum eventID, uint value) {
		SimConnect!.MapClientEventToSimEvent(eventID, eventID.ToString());
		SimConnect.TransmitClientEvent(0U, eventID, value, SimNotificationGroup.Group0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
	}

	public void TransmitEventEX1(Enum eventID, uint value) {
		SimConnect!.MapClientEventToSimEvent(eventID, eventID.ToString());
		SimConnect.TransmitClientEvent_EX1(0U, eventID, SimNotificationGroup.Group0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY, value, 0, 0, 0, 0);
	}

	public void SendThrottleEvent(byte value) {
		TransmitEvent(SimEvent.THROTTLE_SET, (uint) Math.Round(value / 100f * 16384f));
	}

	public void SendMixtureEvent(byte value) {
		TransmitEvent(SimEvent.MIXTURE_SET, (uint) Math.Round(value / 100f * 16384f));
	}

	public void SendSpeedEvent(uint value) {
		TransmitEventEX1(SimEvent.AP_SPD_VAR_SET, value);
	}

	public void SendHeadingEvent(uint value) {
		TransmitEventEX1(SimEvent.HEADING_BUG_SET, value);
	}

	public void SendAltitudeEvent(uint value) {
		TransmitEventEX1(SimEvent.AP_ALT_VAR_SET_ENGLISH, value);
	}

	public void SendVerticalSpeedEvent(int value) {
		TransmitEventEX1(SimEvent.AP_VS_VAR_SET_ENGLISH, (uint) value);
	}

	public void SendTrimEvent(int value) {
		var pizda = value / 90f * (value > 0 ? 16384f : 16383f);

		TransmitEventEX1(SimEvent.AP_VS_VAR_SET_ENGLISH, (uint) pizda);
	}
}
