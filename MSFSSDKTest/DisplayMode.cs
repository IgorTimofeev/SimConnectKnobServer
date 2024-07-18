namespace MSFSSDKTest;

public abstract class DisplayMode {
	public DisplayMode(string title, string suffix, object body) {
		Title = new(title);
		Suffix = new(suffix);
		Body = new(body);
	}

	public static List<DisplayMode> Instances = [];

	public static T Register<T>(T mode) where T : DisplayMode {
		Instances.Add(mode);

		return mode;
	}

	public SerialVar<string>
		Title,
		Suffix;

	public SerialVar<object> Body;

	public abstract void FromRotation(long time, bool right);
	public abstract void SendSimEvent(Sim sim);

	public virtual string BodyToString() => Body.Value.ToString()!;
}


public abstract class IntDisplayMode : DisplayMode {
	public IntDisplayMode(string title, string suffix, int minimum = 0, int maximum = 100) : base(title, suffix, "0") {
		Minimum = minimum;
		Maximum = maximum;
	}

	public int Minimum, Maximum;
	public bool Cycling;

	public virtual int IntValue {
		get => (int) Body.Value;
		set {
			if (value < Minimum) {
				value = Cycling ? Maximum : Minimum;
			}
			else if (value > Maximum) {
				value = Cycling ? Minimum : Maximum;
			}

			Body.Value = value;
		}
	}

	protected void SnapValue(int step, bool right) {
		IntValue = IntValue - IntValue % step + step * (right ? 1 : -1);
	}

	protected void SnapValue(bool right) {
		IntValue += right ? 1 : -1;
	}

	public override void FromRotation(long time, bool right) {
		SnapValue(right);
	}
}


public abstract class BoolDisplayMode : DisplayMode {
	public BoolDisplayMode(string title) : base(title, "", "false") {

	}

	public bool BoolValue {
		get => (bool) Body.Value;
		set => Body.Value = value;
	}

	public override void FromRotation(long time, bool right) {
		BoolValue = !BoolValue;
	}
}

public abstract class PercentDisplayMode : IntDisplayMode {
	public PercentDisplayMode(string title, int minimum = 0, int maximum = 100) : base(title, "%", minimum, maximum) {

	}

	public override void FromRotation(long time, bool right) {
		if (time < 30) {
			SnapValue(10, right);
		}
		else {
			SnapValue(right);
		}
	}
}

public class ElevatorTrimDisplayMode : PercentDisplayMode {
	public ElevatorTrimDisplayMode() : base("EL TRIM", -100, 100) {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendTrimEvent(IntValue);
	}
}

public class ThrottleDisplayMode : PercentDisplayMode {
	public ThrottleDisplayMode() : base("THR") {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendThrottleEvent((byte) IntValue);
	}
}

public class MixtureDisplayMode : PercentDisplayMode {
	public MixtureDisplayMode() : base("MIX") {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendMixtureEvent((byte) IntValue);
	}
}

public class SpeedDisplayMode : IntDisplayMode {
	public SpeedDisplayMode() : base("SPD", "kt", 0, 2500) {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendSpeedEvent((uint) IntValue);
	}
}

public class HeadingDisplayMode : IntDisplayMode {
	public HeadingDisplayMode() : base("HDG", "deg", 0, 360) {
		Cycling = true;
	}

	public override void FromRotation(long time, bool right) {
		if (time < 30) {
			SnapValue(10, right);
		}
		else {
			SnapValue(right);
		}
	}

	public override void SendSimEvent(Sim sim) {
		sim.SendHeadingEvent((uint) IntValue);
	}
}

public class AltitudeDisplayMode : IntDisplayMode {
	public AltitudeDisplayMode() : base("ALT", "ft", 0, 250000) {
		Cycling = true;
	}

	public override void FromRotation(long time, bool right) {
		if (time < 30) {
			SnapValue(1000, right);
		}
		else {
			SnapValue(100, right);
		}
	}

	public override void SendSimEvent(Sim sim) {
		sim.SendAltitudeEvent((uint) IntValue);
	}
}

public class VerticalSpeedDisplayMode : IntDisplayMode {
	public VerticalSpeedDisplayMode() : base("V/S", "ft/min", -2500, 2500) {

	}

	public override void FromRotation(long time, bool right) {
		SnapValue(100, right);
	}

	public override void SendSimEvent(Sim sim) {
		sim.SendVerticalSpeedEvent(IntValue);
	}
}

public class AutopilotDisplayMode : BoolDisplayMode {
	public AutopilotDisplayMode() : base("A/P") {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendAutopilotMasterEvent(BoolValue);
	}
}

public class ParkingBreaksDisplayMode : BoolDisplayMode {
	public ParkingBreaksDisplayMode() : base("PRK BRK") {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendParkingBrakeEvent(BoolValue);
	}
}


public class APFLCDisplayMode : BoolDisplayMode {
	public APFLCDisplayMode() : base("FLC") {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendAPFLCEvent(BoolValue);
	}
}

public class APVSHoldDisplayMode : BoolDisplayMode {
	public APVSHoldDisplayMode() : base("V/S HOLD") {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendAPVSHoldEvent(BoolValue);
	}
}

public class APHDGHoldDisplayMode : BoolDisplayMode {
	public APHDGHoldDisplayMode() : base("HDG HOLD") {

	}

	public override void SendSimEvent(Sim sim) {
		sim.SendAPHDGHoldEvent(BoolValue);
	}
}
