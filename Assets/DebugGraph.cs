using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugGraph : MonoBehaviour {

	[SerializeField]
	private float updateInterval = 0.2f;

	private float nextTimeToUpdate = 0.0f;
	private List<float> values;
	private List<Vector2> graphPoints;
	private ElementEmulator trackedEmulator;


	void Awake() {
		trackedEmulator = FindObjectOfType<ElementEmulator>();
		values = new List<float>();
		graphPoints = new List<Vector2>();
	}

	void Update () {
		if (Time.time < nextTimeToUpdate) return;
		nextTimeToUpdate = Time.time + updateInterval;

		values.Add(trackedEmulator.GetGridDiagnosis());

		float highest = -100000.0f;
		for (int i = 0; i < values.Count; i++){
			float value = values[i];
			if(value > highest) highest = value;
		}

		graphPoints.Add(new Vector2());
		float pointSpread = transform.localScale.x / (float)values.Count;
		for (int i = 0; i < graphPoints.Count; i++){
			Vector2 graphPoint = graphPoints[i];

			graphPoint.x = transform.position.x - (transform.localScale.x * 0.5f) + pointSpread * i;

			graphPoint.y = transform.position.y - transform.localScale.y * 0.5f;
			graphPoint.y += transform.localScale.y * (values[i] / highest);

			if (i > 0){
				Debug.DrawLine(graphPoints[i - 1], graphPoint, Color.magenta, updateInterval);
			}

			graphPoints[i] = graphPoint;
		}
	}
}
