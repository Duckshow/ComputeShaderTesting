using UnityEngine;

public class BuildTool : Singleton<BuildTool> {

	private Mouse instanceMouse;

	private Vector2Int posGridStart;
	private Vector2Int posGridEnd;


	public override bool IsUsingAwakeDefault() { return true; }
	public override void AwakeDefault() {
		instanceMouse = Mouse.GetInstance();
	}

	public override bool IsUsingUpdateDefault() { return true; }
	public override void UpdateDefault() { 
		if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Click){
			posGridStart = instanceMouse.GetPosGrid();
		}
		else if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Hold){
			posGridEnd = instanceMouse.GetPosGrid();
		}
		else if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Release){
			
		}
	}
}
