using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MSFSSDKTest;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SimData {
	public double Throttle;
	public double Mixture;

	public int ElevatorTrim;

	public int ParkingBreak;

	public int APMaster;
	public int APHDGHold;
	public int APFLC;
	public int APVSHold;

	public double APSpeed;
	public double APHeading;
	public double APAltitude;
	public double APVerticalSpeed;
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
	ELEVATOR_TRIM_SET,
	PARKING_BRAKE_SET,
	AP_MASTER,
	AILERON_SET,
	ELEVATOR_SET,
	FLIGHT_LEVEL_CHANGE_ON,
	FLIGHT_LEVEL_CHANGE_OFF,
	AP_HDG_HOLD_ON,
	AP_HDG_HOLD_OFF,
	AP_PANEL_VS_ON,
	AP_PANEL_VS_OFF
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

			MainWindow.ElevatorTrimMode.Body.Value = (int) Math.Round(simData.ElevatorTrim / 16384f * 100f);

			MainWindow.ParkingBreakMode.Body.Value = simData.ParkingBreak == 1;

			MainWindow.APMode.Body.Value = simData.APMaster == 1;
			MainWindow.APHDGHoldMode.Body.Value = simData.APHDGHold == 1;
			MainWindow.APFLCMode.Body.Value = simData.APFLC == 1;
			MainWindow.APVSHoldMode.Body.Value = simData.APVSHold == 1;

			MainWindow.APSPDMode.Body.Value = (int) Math.Round(simData.APSpeed);
			MainWindow.APHDGMode.Body.Value = (int) Math.Round(simData.APHeading);
			MainWindow.APALTMode.Body.Value = (int) Math.Round(simData.APAltitude);
			MainWindow.APVSMode.Body.Value = (int) Math.Round(simData.APVerticalSpeed * 60);

			MainWindow.SerialWriteModeValueChecked();

			MainWindow.GovnoTextBox.Clear();

			foreach (var mode in DisplayMode.Instances)
				MainWindow.GovnoTextBox.AppendText($"{mode.Title.Value}: {mode.Body.Value} {mode.Suffix.Value}\n");
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

			SimConnect.AddToDataDefinition(SimDefinition.SimData, "ELEVATOR TRIM INDICATOR", "position 16K", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

			SimConnect.AddToDataDefinition(SimDefinition.SimData, "BRAKE PARKING POSITION", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT MASTER", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT HEADING LOCK", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT FLIGHT LEVEL CHANGE", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT VERTICAL HOLD", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT AIRSPEED HOLD VAR", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT HEADING LOCK DIR", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT ALTITUDE LOCK VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
			SimConnect.AddToDataDefinition(SimDefinition.SimData, "AUTOPILOT VERTICAL HOLD VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
				
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

	public void TransmitEvent(Enum eventID) {
		TransmitEvent(eventID, 0);
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

	public void SendTrimEvent(int percent) {
		var pizda = percent / 100f * (percent > 0 ? 16384f : 16383f);

		TransmitEvent(SimEvent.ELEVATOR_TRIM_SET, (uint) pizda);
	}

	public void SendParkingBrakeEvent(bool value) {
		TransmitEvent(SimEvent.PARKING_BRAKE_SET, (uint) (value ? 1 : 0));
	}

	public void SendAPFLCEvent(bool value) {
		TransmitEvent(value ? SimEvent.FLIGHT_LEVEL_CHANGE_ON : SimEvent.FLIGHT_LEVEL_CHANGE_OFF);
	}

	public void SendAPHDGHoldEvent(bool value) {
		TransmitEvent(value ? SimEvent.AP_HDG_HOLD_ON : SimEvent.AP_HDG_HOLD_OFF);
	}

	public void SendAPVSHoldEvent(bool value) {
		TransmitEvent(value ? SimEvent.AP_PANEL_VS_ON : SimEvent.AP_PANEL_VS_OFF);
	}

	public void SendAutopilotMasterEvent(bool value) {
		TransmitEvent(SimEvent.AP_MASTER, (uint) (value ? 1 : 0));
	}

	public void SendAileronEvent(int percent) {
		var pizda = percent / 100f * (percent > 0 ? 16384f : 16383f);

		TransmitEvent(SimEvent.AILERON_SET, (uint) pizda);
	}

	public void SendElevatorEvent(int percent) {
		var pizda = percent / 100f * (percent > 0 ? 16384f : 16383f);

		TransmitEvent(SimEvent.ELEVATOR_SET, (uint) pizda);
	}
}
