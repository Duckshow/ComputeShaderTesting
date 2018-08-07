using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementSimulator : MonoBehaviour {

	struct Particle{ // WARNING: variables must correspond to ElementSimulator.compute's Particle!
		public Vector2 Pos;
		public Vector2 Velocity;
		public Vector2 Force;
		public float Density;
		public float Pressure;
		public float Temperature;
		public float TemperatureStartFrame;
		public float RepelFactor;
		public float IsActive; // every thread needs a particle, so some will get inactive particles instead
		public uint ElementIndex;
		public Color ParticlesToHeat;
		public Color HeatToGive;
		public float DebugTemp;
		public float DebugThermal;
		public float Debug1;
		public float Debug2;
		public float Debug3;
		public float Debug4;
		public float Debug5;
		public float Debug6;
		public float Debug7;
		public float Debug8;

		public static int GetStride() {
			return sizeof(float) * 30 + sizeof(uint) * 1; // must correspond to variables!
		}
	}

	private const int THREAD_COUNT_MAX = 1024;
	private const int START_PARTICLE_COUNT = 4096; // must be divisible by THREAD_COUNT_X!
	private const int START_PARTICLE_COUNT_ACTIVE = 4096;

	//#region[rgba(80, 0, 0, 1)] | WARNING: shared with ElementSimulator.compute! must be equal!
	private const int OUTPUT_THREAD_COUNT_X = 32;
	private const int OUTPUT_THREAD_COUNT_Y = 32; 
	
	private const int THREAD_COUNT_X = 64;
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 10;
	private const int GRID_HEIGHT_TILES = 10;
	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
	private const int BIN_WIDTH = 1;
	private const int BIN_COUNT_X = (GRID_WIDTH_PIXELS / BIN_WIDTH) / 2;
	private const int BIN_COUNT_Y = (GRID_HEIGHT_PIXELS / BIN_WIDTH) / 2;
	private const int BIN_MAX_AMOUNT_OF_CONTENT = 64;
	//#endregion

	// kernels
	private const string KERNEL_INIT = "Init";
	private const string KERNEL_RESETTEMPORARYVARIABLES = "ResetTemporaryVariables";
	private const string KERNEL_CLEAROUTPUTTEXTURE = "ClearOutputTexture";
	private const string KERNEL_COMPUTEDENSITYHEATANDPRESSURE = "ComputeDensityHeatAndPressure";
	private const string KERNEL_APPLYHEAT = "ApplyHeat";
	private const string KERNEL_COMPUTEFORCES = "ComputeForces";
	private const string KERNEL_INTEGRATE = "Integrate";
	private int kernelID_Init;
	private int kernelID_ResetTemporaryVariables;
	private int kernelID_ClearOutputTexture;
	private int kernelID_ComputeDensityHeatAndPressure;
	private int kernelID_ApplyHeat;
	private int kernelID_ComputeForces;
	private int kernelID_Integrate;

	// properties
	private const string PROPERTY_PARTICLES = "particles";
	private const string PROPERTY_PARTICLECOUNT = "particleCount";
	private const string PROPERTY_BINLOADS = "binLoads";
	private const string PROPERTY_BINCONTENTS = "binContents";
	private const string PROPERTY_OUTPUT = "output";
	private int shaderPropertyID_particles;
	private int shaderPropertyID_particleCount;
	private int shaderPropertyID_binLoads;
	private int shaderPropertyID_binContents;
	private int shaderPropertyID_output;

	private float updateInterval = 0.0f;
	private float nextTimeToUpdate = 0.0f;

	private ComputeBuffer bufferParticles;
	private Particle[] particles;
	private Texture2D binLoads;
	private Texture2DArray binContents;

	private RenderTexture output;
	private Vector2[] uvs;

	[SerializeField]
	private ComputeShader shader;
	[SerializeField]
	private Material material;
	[SerializeField]
	private ParticleSystem particleSys;

	private int debugIndex = -1;


	[Space]
	[SerializeField]
	private int particleIndex;
	[EasyButtons.Button]
	public void PrintParticle() {
		Debug.Log("=====================================");
		Debug.Log(particleIndex + ": ");
		Debug.Log("IsActive = " + particles[particleIndex].IsActive);
		Debug.Log("Temperature = " + particles[particleIndex].DebugTemp);
		Debug.Log("ThermalDiff = " + particles[particleIndex].DebugThermal);
		Debug.Log("Debug1 = " + particles[particleIndex].Debug1);
		Debug.Log("Debug2 = " + particles[particleIndex].Debug2);
		Debug.Log("Debug3 = " + particles[particleIndex].Debug3);
		Debug.Log("Debug4 = " + particles[particleIndex].Debug4);
		Debug.Log("Debug5 = " + particles[particleIndex].Debug5);
		Debug.Log("Debug6 = " + particles[particleIndex].Debug6);
		Debug.Log("Debug7 = " + particles[particleIndex].Debug7);
		Debug.Log("Debug8 = " + particles[particleIndex].Debug8);
		Debug.Log("=====================================");
	}

	void Awake(){
		kernelID_Init = shader.FindKernel(KERNEL_INIT);
		kernelID_ResetTemporaryVariables = shader.FindKernel(KERNEL_RESETTEMPORARYVARIABLES);
		kernelID_ClearOutputTexture = shader.FindKernel(KERNEL_CLEAROUTPUTTEXTURE);
		kernelID_ComputeDensityHeatAndPressure = shader.FindKernel(KERNEL_COMPUTEDENSITYHEATANDPRESSURE);
		kernelID_ApplyHeat = shader.FindKernel(KERNEL_APPLYHEAT);
		kernelID_ComputeForces = shader.FindKernel(KERNEL_COMPUTEFORCES);
		kernelID_Integrate = shader.FindKernel(KERNEL_INTEGRATE);

		shaderPropertyID_particles = Shader.PropertyToID(PROPERTY_PARTICLES);
		shaderPropertyID_particleCount = Shader.PropertyToID(PROPERTY_PARTICLECOUNT);
		shaderPropertyID_binLoads = Shader.PropertyToID(PROPERTY_BINLOADS);
		shaderPropertyID_binContents = Shader.PropertyToID(PROPERTY_BINCONTENTS);
		shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
	}

	void OnDisable(){
		bufferParticles.Dispose();
	}
	
	void Start () {
		InitShader();
		particleSys.Play();
		particleSys.Emit(particles.Length);
	}

	void InitShader(){
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		particles = new Particle[START_PARTICLE_COUNT];
		binLoads = new Texture2D(BIN_COUNT_X, BIN_COUNT_Y, TextureFormat.RGBA32, mipmap: false);
		binContents = new Texture2DArray(BIN_COUNT_X, BIN_COUNT_Y, BIN_MAX_AMOUNT_OF_CONTENT, TextureFormat.RGBA32, mipmap: false);
		
		output = new RenderTexture(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS, 24);
		output.enableRandomWrite = true;
		output.filterMode = FilterMode.Point;
		output.Create();

		bool reverse = false;
		int x = 0, y = 0;
		for (int i = 0; i < particles.Length; i++){
			if (i > 0){
				if (!reverse && i >= particles.Length * 0.5f){
					reverse = true;
					y = GRID_HEIGHT_PIXELS;
					x = GRID_WIDTH_PIXELS - 1;
				}

				if (reverse){
					y--;
					if (y < 0){
						y = GRID_HEIGHT_PIXELS - 1;
						x--;
					}
				}
				else{
					y++;
					if (y == GRID_HEIGHT_PIXELS){
						y = 0;
						x++;
					}
				}
			}

			Particle particle = particles[i];

			float jitterX = Random.value * 0.1f;
			particle.Pos = new Vector2(reverse ? x - jitterX : x + jitterX, y);
			//particle.Temperature = Random.Range(0, 1000);
			particle.Temperature = reverse ? 1000 : 0;
			particle.TemperatureStartFrame = particle.Temperature;
			particle.ElementIndex = 0;
			//particle.IsActive = i == 0 || i == 100 ? 1 : 0;
			particle.IsActive = Mathf.Clamp01(Mathf.Sign(START_PARTICLE_COUNT_ACTIVE - (i + 1)));

			particles[i] = particle;
		}
		while (debugIndex < 0 || particles[debugIndex].IsActive == 0){
			debugIndex = Random.Range(0, particles.Length);
		}
		// particles[0].Temperature = 100000;
		// particles[0].TemperatureStartFrame = 100000;
		bufferParticles = new ComputeBuffer(particles.Length, Particle.GetStride());

		// Init
		bufferParticles.SetData(particles);
		shader.SetBuffer(kernelID_Init, shaderPropertyID_particles, bufferParticles);
		shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		shader.Dispatch(kernelID_Init, Mathf.CeilToInt(particles.Length / THREAD_COUNT_X), 1, 1);
		bufferParticles.GetData(particles);
	}

	void Update() {
		if (Time.time < nextTimeToUpdate) return;
		nextTimeToUpdate = Time.time + updateInterval;
		UpdateShader();
	}

	void UpdateShader() {
		int threadGroupCountX = Mathf.CeilToInt(particles.Length / THREAD_COUNT_X);

		// ResetTemporaryVariables
		bufferParticles.SetData(particles);
		shader.SetBuffer(kernelID_ResetTemporaryVariables, shaderPropertyID_particles, bufferParticles);
		shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		shader.Dispatch(kernelID_ResetTemporaryVariables, threadGroupCountX, 1, 1);
		//bufferParticles.GetData(particles);

		int outputThreadGroupCountX = Mathf.CeilToInt(GRID_WIDTH_PIXELS / OUTPUT_THREAD_COUNT_X);
		int outputThreadGroupCountY = Mathf.CeilToInt(GRID_HEIGHT_PIXELS / OUTPUT_THREAD_COUNT_Y);
		shader.SetTexture(kernelID_ClearOutputTexture, shaderPropertyID_output, output);
		shader.Dispatch(kernelID_ClearOutputTexture, outputThreadGroupCountX, outputThreadGroupCountY, 1);

		// ComputeDensityAndPressure
		//bufferParticles.SetData(particles);
		//shader.SetBuffer(kernelID_ComputeDensityHeatAndPressure, shaderPropertyID_particles, bufferParticles);
		//shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		// NOTE: bintextures may have to be rendertextures in order to write...
		// shader.SetTexture(kernelID_ComputeDensityHeatAndPressure, shaderPropertyID_binLoads, binLoads); 
		// shader.SetTexture(kernelID_ComputeDensityHeatAndPressure, shaderPropertyID_binContents, binContents);

		shader.Dispatch(kernelID_ComputeDensityHeatAndPressure, threadGroupCountX, 1, 1);
		//bufferParticles.GetData(particles);

		// ApplyHeat
		//bufferParticles.SetData(particles);
		//shader.SetBuffer(kernelID_ApplyHeat, shaderPropertyID_particles, bufferParticles);
		//shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		shader.Dispatch(kernelID_ApplyHeat, threadGroupCountX, 1, 1);
		//bufferParticles.GetData(particles);

		// ComputeForces
		//bufferParticles.SetData(particles);
		//shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_particles, bufferParticles);
		//shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		// NOTE: bintextures may have to be rendertextures in order to write...
		// shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_binLoads, binLoads);
		// shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_binContents, binContents);

		shader.Dispatch(kernelID_ComputeForces, threadGroupCountX, 1, 1);
		//bufferParticles.GetData(particles);

		// Integrate
		//bufferParticles.SetData(particles);
		//shader.SetBuffer(kernelID_Integrate, shaderPropertyID_particles, bufferParticles);
		//shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		// NOTE: bintextures may have to be rendertextures in order to write...
		// shader.SetTexture(kernelID_Integrate, shaderPropertyID_binLoads, binLoads);
		// shader.SetTexture(kernelID_Integrate, shaderPropertyID_binContents, binContents);

		shader.SetTexture(kernelID_Integrate, shaderPropertyID_output, output);

		shader.Dispatch(kernelID_Integrate, threadGroupCountX, 1, 1);
		bufferParticles.GetData(particles);

		//material.mainTexture = output;


		ParticleSystem.Particle[] unityParticles = new ParticleSystem.Particle[particles.Length];
		int particleCount = particleSys.GetParticles(unityParticles);
		for (int i = 0; i < unityParticles.Length; i++){
			Particle particle = particles[i];
			ParticleSystem.Particle unityParticle = unityParticles[i];

			Vector2 worldPos = particle.Pos;
			worldPos.x = worldPos.x / GRID_WIDTH_PIXELS * GRID_WIDTH_TILES;
			worldPos.y = worldPos.y / GRID_HEIGHT_PIXELS * GRID_HEIGHT_TILES;
			worldPos.x -= GRID_WIDTH_TILES * 0.5f;
			worldPos.y -= GRID_HEIGHT_TILES * 0.5f;
			unityParticle.position = worldPos;

			Color color = Color.Lerp(Color.blue, Color.red, particle.Temperature / 1000.0f);
			if (particleIndex == i){
				color = Color.green;
			}

			//Color color = Color.Lerp(Color.blue, Color.red, particle.Debug1 / 8);// particle.Temperature / 1000.0f);
			//color.a = 1 - Mathf.Abs(Mathf.Clamp(i - debugIndex, -1, 1));// particle.IsActive;
			color.a = particle.IsActive;
			unityParticle.startColor = color;

			unityParticles[i] = unityParticle;
		}
		particleSys.SetParticles(unityParticles, particleCount);

		float total = 0;
		float highest = -10000;
		float highestIndex = -1;
		float lowest = 10000;
		float lowestIndex = -1;
		int debugIndex = Random.Range(0, particles.Length);
		for (int i = 0; i < particles.Length; i++){
			Particle debugParticle = particles[i];
			if(debugParticle.IsActive == 0) continue;
			if (debugParticle.Temperature > highest){
				highest = debugParticle.Temperature;
				highestIndex = i;
			}
			if (debugParticle.Temperature < lowest){
				lowest = debugParticle.Temperature;
				lowestIndex = i;
			}

			// if (debugParticle.Debug1 > 0){// || debugParticle.Debug2 > 0 || debugParticle.Debug3 > 0 || debugParticle.Debug4 > 0){
			// 	Debug.LogFormat(i + ": (heatToGive)({0}, {1}, {2}, {3}), (particlesToHeat)({4}, {5}, {6}, {7})", debugParticle.Debug1, debugParticle.Debug2, debugParticle.Debug3, debugParticle.Debug4, debugParticle.Debug5, debugParticle.Debug6, debugParticle.Debug7, debugParticle.Debug8);
			// }
			total += debugParticle.Temperature;
		}
		//Debug.Log("Total: " + total + " (High: (" + highestIndex + ") " + highest + ", Low: (" + lowestIndex + ") " + lowest + ")");
	}
}