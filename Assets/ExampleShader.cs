using UnityEngine;
public class ExampleShader : MonoBehaviour {

//#region[rgba(50, 0, 0, 1)] WARNING: Must correspond to variables in ExampleShader.compute!
	// struct Ball{
	// 	public Vector2 pos;

	// 	public static int GetStride(){
	// 		return sizeof(float) * 2;
	// 	}
	// }

	private const int THREADS_MAIN = 1024;
	// private const int BALL_COUNT = 10240;
//#endregion
	private const int THREADGROUPS_MAIN = 1;// BALL_COUNT / THREADS_MAIN;

	// [SerializeField] private GameObject ballObjectPrefab;
	[SerializeField] private ComputeShader shader;

	private int kernelID_Main;
	// private float time;

	// private GameObject[] ballObjects = new GameObject[BALL_COUNT];
	// private Ball[] balls = new Ball[BALL_COUNT];
	// private ComputeBuffer bufferBalls;


	// void Awake(){
	// 	for (int i = 0; i < BALL_COUNT; i++){
	// 		GameObject newBallObject = Instantiate(ballObjectPrefab, transform.position, Quaternion.identity);
	// 		ballObjects[i] = newBallObject;
	// 	}
	// }

	// void OnDisable(){
	// 	bufferBalls.Dispose();
	// }

	void Start () {
		kernelID_Main = shader.FindKernel("Main");

		// bufferBalls = new ComputeBuffer(BALL_COUNT, Ball.GetStride());
		// bufferBalls.SetData(balls);
		// shader.SetBuffer(kernelID_Main, "balls", bufferBalls);
	}
	
	void Update () {
		// time += Time.deltaTime;
		// shader.SetFloat("time", time);

		shader.Dispatch(kernelID_Main, THREADGROUPS_MAIN, 1, 1);

		// bufferBalls.GetData(balls);
		// for (int i = 0; i < BALL_COUNT; i++){
		// 	ballObjects[i].transform.position = balls[i].pos;
		// }
	}
}
