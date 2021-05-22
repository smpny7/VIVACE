using UnityEngine;

public static class GameUtil {
	public static KeyCode GetKeyCodeByLineNum (int lineNum) =>
		lineNum switch
		{
			0 => KeyCode.D,
			1 => KeyCode.F,
			2 => KeyCode.G,
			3 => KeyCode.H,
			4 => KeyCode.J,
			_ => KeyCode.None
		};
}