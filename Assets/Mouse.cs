using UnityEngine;

public class Mouse : Singleton<Mouse> {
	private const int SUPPORTED_MOUSE_BUTTON_COUNT = 2;
	private const float MIN_HOLD_TIME = 0.3f;

	public enum StateEnum { Idle, Click, Hold, Release }

	private StateEnum StateLMB;
	private StateEnum StateRMB;
	public StateEnum GetStateLMB() { return StateLMB; }
	public StateEnum GetStateRMB() { return StateRMB; }

	private Vector2 pos;
	private Vector2Int posGrid;
	public Vector2Int GetPosGrid() { return posGrid; }

	private float[] timeAtClicks = new float[SUPPORTED_MOUSE_BUTTON_COUNT];
	private Vector2Int[] posGridAtClicks = new Vector2Int[SUPPORTED_MOUSE_BUTTON_COUNT];


	public override bool IsUsingUpdateEarly(){ return true; }
	public override void UpdateEarly(){
		base.UpdateEarly();

		pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		posGrid = ShipGrid.ConvertWorldToGrid(pos);

		StateLMB = GetMouseButtonState(0);
		StateRMB = GetMouseButtonState(1);
	}

	StateEnum GetMouseButtonState(int index) { 
		if (Input.GetMouseButtonDown(index)){
			timeAtClicks[index] = Time.time;
			posGridAtClicks[index] = posGrid;
			return StateEnum.Click;
		}

		bool hasPassedMinHoldTime = Time.time - timeAtClicks[index] > MIN_HOLD_TIME;
		bool hasChangedPosGrid = posGrid != posGridAtClicks[index];
		if (Input.GetMouseButton(index) && (hasPassedMinHoldTime || hasChangedPosGrid)){
			return StateEnum.Hold;
		}

		if (Input.GetMouseButtonUp(index)){
			return StateEnum.Release;
		}
		
		return StateEnum.Idle;
	}
}
