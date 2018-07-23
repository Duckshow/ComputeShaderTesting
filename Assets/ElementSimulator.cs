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

		public static int GetStride() {
			return sizeof(float) * 12 + sizeof(uint) * 1; // must correspond to variables!
		}
	}

	private const int THREAD_COUNT_MAX = 1024;
	private const int START_PARTICLE_COUNT = 64; // must be divisible by THREAD_COUNT_X!
	private const int START_PARTICLE_COUNT_ACTIVE = 60;

	//#region[rgba(80, 0, 0, 1)] | WARNING: shared with ElementSimulator.compute! must be equal!
	private const int THREAD_COUNT_X = 64;
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 2;
	private const int GRID_HEIGHT_TILES = 1;
	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
	private const int BIN_WIDTH = 1;
	private const int BIN_COUNT_X = (GRID_WIDTH_PIXELS / BIN_WIDTH) / 2;
	private const int BIN_COUNT_Y = (GRID_HEIGHT_PIXELS / BIN_WIDTH) / 2;
	private const int BIN_MAX_AMOUNT_OF_CONTENT = 64;
	//#endregion

	// kernels
	private const string KERNEL_COMPUTEDENSITYANDPRESSURE = "ComputeDensityAndPressure";
	private const string KERNEL_COMPUTEFORCES = "ComputeForces";
	private const string KERNEL_INTEGRATE = "Integrate";
	private int kernelID_ComputeDensityAndPressure;
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


	void Awake(){
		kernelID_ComputeDensityAndPressure = shader.FindKernel(KERNEL_COMPUTEDENSITYANDPRESSURE);
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
	}

	void InitShader(){
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		output = new RenderTexture(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS, 24);
		output.enableRandomWrite = true;
		output.filterMode = FilterMode.Point;
		output.Create();

		binLoads = new Texture2D(BIN_COUNT_X, BIN_COUNT_Y, TextureFormat.RGBA32, mipmap: false);
		binContents = new Texture2DArray(BIN_COUNT_X, BIN_COUNT_Y, BIN_MAX_AMOUNT_OF_CONTENT, TextureFormat.RGBA32, mipmap: false);

		particles = new Particle[START_PARTICLE_COUNT];
		int x = 0, y = 0;
		for (int i = 0; i < particles.Length; i++){
			if (i > 0){
				y++;
				if (y == GRID_HEIGHT_PIXELS){
					y = 0;
					x++;
				}
			}
			
			Particle particle = particles[i];

			float jitterX = 0;//Random.value * 10;
			particle.Pos = new Vector2(x + jitterX, y);
			particle.Temperature = Mathf.Round(Random.value);//0.0f;
			particle.ElementIndex = 0;
			particle.IsActive = Mathf.Clamp01(Mathf.Sign(START_PARTICLE_COUNT_ACTIVE - (i + 1)));

			particles[i] = particle;
		}
		bufferParticles = new ComputeBuffer(particles.Length, Particle.GetStride());
	}

	void Update() {
		if (Time.time < nextTimeToUpdate) return;
		nextTimeToUpdate = Time.time + updateInterval;
		UpdateShader();
	}

	void UpdateShader() {
		int threadGroupCountX = Mathf.CeilToInt(particles.Length / THREAD_COUNT_X);

		// ComputeDensityAndPressure
		bufferParticles.SetData(particles);
		shader.SetBuffer(kernelID_ComputeDensityAndPressure, shaderPropertyID_particles, bufferParticles);
		shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		// NOTE: bintextures may have to be rendertextures in order to write...
		shader.SetTexture(kernelID_ComputeDensityAndPressure, shaderPropertyID_binLoads, binLoads); 
		shader.SetTexture(kernelID_ComputeDensityAndPressure, shaderPropertyID_binContents, binContents);

		shader.Dispatch(kernelID_ComputeDensityAndPressure, threadGroupCountX, 1, 1);
		bufferParticles.GetData(particles);

		// ComputeForces
		bufferParticles.SetData(particles);
		shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_particles, bufferParticles);
		shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		// NOTE: bintextures may have to be rendertextures in order to write...
		shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_binLoads, binLoads);
		shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_binContents, binContents);

		shader.Dispatch(kernelID_ComputeForces, threadGroupCountX, 1, 1);
		bufferParticles.GetData(particles);

		// Integrate
		bufferParticles.SetData(particles);
		shader.SetBuffer(kernelID_Integrate, shaderPropertyID_particles, bufferParticles);
		shader.SetInt(shaderPropertyID_particleCount, particles.Length);
		// NOTE: bintextures may have to be rendertextures in order to write...
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_binLoads, binLoads);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_binContents, binContents);

		shader.SetTexture(kernelID_Integrate, shaderPropertyID_output, output);

		shader.Dispatch(kernelID_Integrate, threadGroupCountX, 1, 1);
		bufferParticles.GetData(particles);

		material.mainTexture = output;


		Debug.Log(particles[10].Pos); oamroäe // try to figure out why nothing is changing over time...
	}

#region C# version of SPH
// 	struct ElementParticle{
// 		private const float THERMAL_DIFFUSIVITY_SOLID = 1.0f;
// 		private const float THERMAL_DIFFUSIVITY_LIQUID = 0.75f;
// 		private const float THERMAL_DIFFUSIVITY_GAS = 0.5f;
// 		private const float STATE_CORRECTION_OFFSET = 0.000001f; // offset to ensure two states can't be == 1 at the same time
// 		private const float REPEL_STRENGTH_SMOOTHING_START_SOLID = 0.9f;

// 		public Vector2 pos;
// 		public Vector2 velocity; 
// 		public Vector2 force;
// 		public float density;
// 		public float pressure;
// 		public float mass;
// 		public float visc;
// 		public int elementIndex;
// 		public int bucketIndex;
// 		public float temperatureFreezingPoint;
// 		public float temperatureBoilingPoint;
// 		public float repelStrengthSolid;
// 		public float repelStrengthLiquid;
// 		public float repelStrengthGas;
// 		public bool debug;
// 		private float temperature;
// 		private float temperatureStartFrame;
// 		private float repelFactor;

// 		public ElementParticle(int elementIndex, float x, float y, float mass, float visc, float temperature, float temperatureFreezingPoint, float temperatureBoilingPoint, float repelStrengthSolid, float repelStrengthLiquid, float repelStrengthGas) {
// 			pos = new Vector2(x, y);
// 			velocity = new Vector2();
// 			force = new Vector2();
// 			density = 0.0f;
// 			pressure = 0.0f;
// 			this.mass = mass;
// 			this.visc = visc;
// 			this.elementIndex = elementIndex;
// 			bucketIndex = -1;
// 			debug = false;

// 			this.temperature = 0.0f;
// 			this.temperatureStartFrame = 0.0f;
// 			this.temperatureFreezingPoint = temperatureFreezingPoint;
// 			this.temperatureBoilingPoint = temperatureBoilingPoint;
// 			this.repelStrengthSolid = repelStrengthSolid;
// 			this.repelStrengthLiquid = repelStrengthLiquid;
// 			this.repelStrengthGas = repelStrengthGas;
// 			repelFactor = 0.0f;
// 			SetTemperature(temperature);
// 		}

// 		public void SetTemperature(float temp){
// 			temperature = Mathf.Clamp(temp, 0.0f, MAX_TEMPERATURE);
// 			//Debug.Log(temperature + " (" + temp + ")");

// 			float isSolid = IsSolid();
// 			float isLiquid = IsLiquid();
// 			float isGas = IsGas();

// 			if (isSolid + isLiquid + isGas != 1.0f){
// 				Debug.Log("Something went wrong! Temperature: " + temperature + ": S=" + isSolid + ", L=" + isLiquid + ", G=" + isGas);
// 			}

// 			// to prevent melting causing explosions, lerp the repelstrength
// 			float repelStrengthSmoothedSolidToLiquid = Mathf.Lerp(repelStrengthSolid, repelStrengthLiquid, (temp - FREEZINGPOINT_WATER * REPEL_STRENGTH_SMOOTHING_START_SOLID) / (FREEZINGPOINT_WATER - FREEZINGPOINT_WATER * REPEL_STRENGTH_SMOOTHING_START_SOLID));
// 			float repelStrengthSmoothedLiquidToGas = Mathf.Lerp(repelStrengthLiquid, repelStrengthGas, (temp - BOILINGPOINT_WATER * REPEL_STRENGTH_SMOOTHING_START_SOLID) / (BOILINGPOINT_WATER - BOILINGPOINT_WATER * REPEL_STRENGTH_SMOOTHING_START_SOLID));

// 			// each state has a fixed strength, but gas continues the more temperature increases
// 			repelFactor = 0.0f;
// 			repelFactor += (1.0f / repelStrengthSmoothedSolidToLiquid) * isSolid;
// 			repelFactor += (1.0f / repelStrengthSmoothedLiquidToGas) * isLiquid;
// 			repelFactor += (1.0f / repelStrengthGas) * isGas;

// 			float extraRepelFactor = 1.0f / Mathf.Max(REPEL_STRENGTH_MIN, temperature / MAX_TEMPERATURE * REPEL_STRENGTH_MAX);
// 			repelFactor += (extraRepelFactor - repelFactor) * isGas;

// 			repelFactor = Mathf.Clamp(repelFactor, REPEL_FACTOR_MIN, REPEL_FACTOR_MAX); // just a safeguard
// 		}

// 		public float GetTemperature(){
// 			return temperature;
// 		}

// 		public void SetTemperatureStartFrame(float temp) {
// 			temperatureStartFrame = temp;
// 		}
		
// 		public float GetTemperatureStartFrame(){
// 			return temperatureStartFrame;
// 		}

// 		public float GetRepelFactor(){
// 			return repelFactor;
// 		}

// 		public float IsSolid() {
// 			return Mathf.Max(0, Mathf.Sign(temperatureFreezingPoint + STATE_CORRECTION_OFFSET - temperature));
// 		}

// 		public float IsLiquid() {
// 			return Mathf.Max(0, Mathf.Sign(temperatureBoilingPoint - STATE_CORRECTION_OFFSET - temperature)) - IsSolid();
// 		}

// 		float IsGas() {
// 			return Mathf.Max(0, Mathf.Sign(temperature - temperatureBoilingPoint + STATE_CORRECTION_OFFSET));
// 		}

// 		public float GetThermalDiffusivity() {
// 			return THERMAL_DIFFUSIVITY * ((IsSolid() * THERMAL_DIFFUSIVITY_SOLID) + (IsLiquid() * THERMAL_DIFFUSIVITY_LIQUID) + (IsGas() * THERMAL_DIFFUSIVITY_GAS));
// 		}
// 	};

// 	// solver parameters
// 	private static readonly Vector2 G = new Vector2(0, 0);//300 * -9.8f); // external (gravitational) forces
// 	private const float REST_DENS = 1000.0f; // rest density
// 	private const float GAS_CONST = 2000.0f; // const for equation of state
// 	private const float H = 0.5f; // interaction radius
// 	private const float HSQ = H * H; // radius^2 for optimization
// 	private const float HSQ_TEMPERATURE = HSQ * 2.0f; // interaction radius
// 	private const float H_SURFACE_TENSION = H * 3.0f; // interaction radius
// 	private const float MASS = 2.0f; // assume all particles have the same mass
// 	private const float VISC = 250.0f; // viscosity constant
// 	private const float DT = 0.0008f; // integration timestep
// 	private const float DENSITY_OFFSET = 0.925f; // make SPIKY_GRAD apply force earlier (particles don't have to be as close)
// 	private const float VISC_OFFSET = 0.925f; // make VISC_LAP apply force earlier (particles don't have to be as close)

// 	// smoothing kernels defined in Müller and their gradients
// 	private static readonly float POLY6 = 315.0f / (65.0f * Mathf.PI * Mathf.Pow(H, 9.0f));
// 	private static readonly float SPIKY_GRAD = -45.0f / (Mathf.PI * Mathf.Pow(H * DENSITY_OFFSET, 6.0f));
// 	private static readonly float VISC_LAP = 45.0f / (Mathf.PI * Mathf.Pow(H * VISC_OFFSET, 6.0f));

// 	// simulation parameters
// 	private const float EPS = H; // boundary epsilon
// 	private const float BOUND_DAMPING = -0.5f;
// 	private const float BOUND_TEMPERATURE = FREEZINGPOINT_WATER + 20.0f;
// 	private const float BOUND_THERMAL_DIFFUSIVITY = 1.0f;
// 	private const int DAM_PARTICLES_L = 100;
// 	private const int DAM_PARTICLES_R = 100;
// 	private const int MAX_PARTICLES = 2000;

// 	// ============== WARNING: shared with ElementEmulator.compute! must be equal!
// 	private const int PIXELS_PER_TILE_EDGE = 32;
// 	private const int GRID_WIDTH_TILES = 2;
// 	private const int GRID_HEIGHT_TILES = 1;
// 	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
// 	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
// 	//===============

// 	private const float MIN_TIME_BETWEEN_PARTICLE_SPAWN = 0.1f;
// 	private const float MAX_TEMPERATURE = 1000.0f;

// 	private const float THERMAL_DIFFUSIVITY = 0.5f;
// 	private const float REPEL_STRENGTH_MIN = 1.0f;
// 	private const float REPEL_STRENGTH_MAX = 8.0f;
// 	private const float REPEL_FACTOR_MIN = 1.0f / REPEL_STRENGTH_MAX;
// 	private const float REPEL_FACTOR_MAX = 1.0f / REPEL_STRENGTH_MIN;
// 	private const float CLUSTERING_RESISTANCE = 1.0f;
// 	private const float SURFACE_TENSION_WATER = 5.0f;

// 	private ElementParticle[] particles;

// 	[SerializeField]
// 	private ComputeShader shader;
// 	[SerializeField]
// 	private Material material;
//     [SerializeField]
//     private ParticleSystem particleSystem;
// 	[SerializeField]
// 	private float mass0 = 1.0f;
// 	[SerializeField]
// 	private float mass1 = 1.0f;
// 	[SerializeField]
// 	private float visc0 = 1.0f;
// 	[SerializeField]
// 	private float visc1 = 1.0f;

// 	private const float REPEL_STRENGTH_SOLID_WATER = 1.75f;
// 	private const float REPEL_STRENGTH_LIQUID_WATER = 1.5f;
// 	private const float REPEL_STRENGTH_GAS_WATER = 2.0f;
// 	private const float FREEZINGPOINT_WATER = 273.15f;
// 	private const float BOILINGPOINT_WATER = 373.15f;

// 	private List<ElementParticle> startParticles;
// 	private ParticleSystem.Particle[] emittedParticles;
// 	private float nextTimeSpawnIsAllowed = 0.0f;


// 	void Start () {
// 		InitSPH();
// 		InitShader();
// 	}

// 	void InitShader() { 
// 		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);
// 	}

// 	void InitSPH(){
// 		mass0 *= MASS;
// 		mass1 *= MASS;
// 		visc0 *= VISC;
// 		visc1 *= VISC;

// 		for (int i = 0; i < particleBuckets.Length; i++){
// 			particleBuckets[i] = new Bucket();
// 		}

// 		startParticles = new List<ElementParticle>();

// 		int addedCount = 0;
// 		float spacing = H * 1.25f;
// 		for (float x = EPS; x < GRID_WIDTH_PIXELS * 0.5f - EPS * 2.0f; x += spacing) {
// 			for (float y = EPS; y < GRID_HEIGHT_PIXELS - EPS * 2.0f; y += spacing) {
// 				if (addedCount < Mathf.Min(DAM_PARTICLES_L, MAX_PARTICLES)){
// 					float jitter = Random.value / spacing * 0.01f;
// 					startParticles.Add(new ElementParticle(0, x + jitter, y, mass0, visc0, FREEZINGPOINT_WATER + 50.0f, FREEZINGPOINT_WATER, BOILINGPOINT_WATER, REPEL_STRENGTH_SOLID_WATER, REPEL_STRENGTH_LIQUID_WATER, REPEL_STRENGTH_GAS_WATER));
// 					addedCount++;
// 				}
// 			}
// 		}

// 		addedCount = 0;
// 		for (float x = GRID_WIDTH_PIXELS - EPS * 4.0f; x > EPS; x -= spacing) {
// 			for (float y = GRID_HEIGHT_PIXELS - EPS * 4.0f; y > EPS; y -= spacing) {
// 				if (addedCount < Mathf.Min(DAM_PARTICLES_R, MAX_PARTICLES)){
// 					float jitter = Random.value / spacing * 0.01f;
// 					startParticles.Add(new ElementParticle(1, x + jitter, y, mass1, visc1, FREEZINGPOINT_WATER + 50.0f, FREEZINGPOINT_WATER, BOILINGPOINT_WATER, REPEL_STRENGTH_SOLID_WATER, REPEL_STRENGTH_LIQUID_WATER, REPEL_STRENGTH_GAS_WATER));
// 					addedCount++;
// 				}
// 			}
// 		}

// 		particles = startParticles.ToArray();
// 	}

// 	void Update(){
// 		if (particles.Length == MAX_PARTICLES) {
// 			return;
// 		}
// 		if (Time.time < nextTimeSpawnIsAllowed){
// 			return;
// 		}
// 		nextTimeSpawnIsAllowed = Time.time + MIN_TIME_BETWEEN_PARTICLE_SPAWN;

// 		float mousePosX;
// 		float mousePosY;
// 		GetMousePositionOnGrid(out mousePosX, out mousePosY);

// 		if (mousePosX < 0 || mousePosY < 0 || mousePosX >= transform.position.x + GRID_WIDTH_PIXELS || mousePosY >= transform.position.y + GRID_HEIGHT_PIXELS){
// 			return;
// 		}

// 		if (Input.GetKey(KeyCode.Mouse0)){
// 			startParticles = new List<ElementParticle>(particles);
// 			ElementParticle particle = new ElementParticle(0, mousePosX, mousePosY, mass0, visc0, MAX_TEMPERATURE, FREEZINGPOINT_WATER, BOILINGPOINT_WATER, REPEL_STRENGTH_SOLID_WATER, REPEL_STRENGTH_LIQUID_WATER, REPEL_STRENGTH_GAS_WATER);
// 			particle.velocity.x = 1000.0f;
// 			particle.debug = true;
// 			startParticles.Add(particle);
// 			particles = startParticles.ToArray();
// 		}

// 		if (Input.GetKey(KeyCode.Mouse1)){
// 			startParticles = new List<ElementParticle>(particles);
// 			ElementParticle particle = new ElementParticle(1, mousePosX, mousePosY, mass1, visc1, 0.0f, FREEZINGPOINT_WATER, BOILINGPOINT_WATER, REPEL_STRENGTH_SOLID_WATER, REPEL_STRENGTH_LIQUID_WATER, REPEL_STRENGTH_GAS_WATER);
// 			particle.velocity.x = -1000.0f;
// 			particle.debug = true;
// 			startParticles.Add(particle);
// 			particles = startParticles.ToArray();
// 		}
		
// 		if (Input.mouseScrollDelta.y != 0){
// 			for (int i = 0; i < particles.Length; i++){
// 				Vector3 screenPos = Camera.main.WorldToScreenPoint(particles[i].pos);
// 				if ((Input.mousePosition - screenPos).magnitude > 20.0f) continue;

// 				float newTemperature = particles[i].GetTemperature() + Input.mouseScrollDelta.y * 100.0f;
// 				newTemperature = Mathf.Clamp(newTemperature, 0, MAX_TEMPERATURE);
// 				particles[i].SetTemperature(newTemperature);
// 			}
// 		}

// 		float temperatureDelta = 20.0f;
// 		if (Input.GetKey(KeyCode.Q)){
// 			for (int i = 0; i < particles.Length; i++){
// 				float newTemperature = particles[i].GetTemperature() - temperatureDelta;
// 				newTemperature = Mathf.Clamp(newTemperature, 0, MAX_TEMPERATURE);
// 				particles[i].SetTemperature(newTemperature);
// 			}
// 		}
		
// 		if (Input.GetKey(KeyCode.E)){
// 			for (int i = 0; i < particles.Length; i++){
// 				float newTemperature = particles[i].GetTemperature() + temperatureDelta;
// 				newTemperature = Mathf.Clamp(newTemperature, 0, MAX_TEMPERATURE);
// 				particles[i].SetTemperature(newTemperature);
// 			}
// 		}
// 	}

// 	void GetMousePositionOnGrid(out float x, out float y){
// 		Vector2 gridPosStart = Camera.main.WorldToScreenPoint(transform.position);
// 		Vector2 gridPosEnd = Camera.main.WorldToScreenPoint(transform.position + new Vector3(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS));
// 		float mousePosX01 = (Input.mousePosition.x - gridPosStart.x) / gridPosEnd.x;
// 		float mousePosY01 = (Input.mousePosition.y - gridPosStart.y) / gridPosEnd.y;
// 		x = GRID_WIDTH_PIXELS * mousePosX01;
// 		y = GRID_HEIGHT_PIXELS * mousePosY01;
// 	}

// 	void FixedUpdate () {
// 		CacheParticlesInBuckets();
// 		ComputeDensityAndPressure();
// 		ComputeForces();
// 		Integrate();
// 		// for (int i = 0; i < particles.Length; i++){
// 		// 	int[] neighbors = GetNeighborParticleIndices(particles[i].pos);
// 		// }

//         particleSystem.Clear();
//         ParticleSystem.MainModule main = particleSystem.main;
//         main.startSize = H * 2;
//         particleSystem.Emit(particles.Length);

// 		if (emittedParticles == null || emittedParticles.Length != particleSystem.particleCount){
// 			emittedParticles = new ParticleSystem.Particle[particleSystem.particleCount];
// 		}
//         particleSystem.GetParticles(emittedParticles);
//         for (int i = 0; i < particleSystem.particleCount; i++){
//             emittedParticles[i].position = particles[i].pos;
// 			if (i == 0){
// //				Debug.Log(particles[i].temperature);
// 				emittedParticles[i].startColor = Color.cyan;
// 			}
// 			else{
// 				// ElementParticle particle = particles[i];
// 				// emittedParticles[i].startColor = Color.Lerp(Color.blue, Color.red, (particle.GetTemperature() - particle.temperatureFreezingPoint) / particle.temperatureBoilingPoint);

// 				emittedParticles[i].startColor = Color.Lerp(Color.blue, Color.red, particles[i].GetTemperature() / MAX_TEMPERATURE);
// 			}
// 		}
//         particleSystem.SetParticles(emittedParticles, particleSystem.particleCount);

// 		// for (int i = 0; i < particleBuckets.Length; i++){
// 		// 	int length = particleBuckets[i].GetContentAmount();
// 		// 	if(length > 0) Debug.Log(length);
// 		// }
// 		//Debug.Log(HYPOTHETICAL_MAX_AMOUNT_PARTICLES + ", " + PARTICLE_BUCKET_COUNT);

// 		//Debug.LogError("");

// 	}
	
// 	void ComputeDensityAndPressure() {
// 		for (int i = 0; i < particles.Length; i++){
// 			ElementParticle particle = particles[i];

// 			particle.density = 0.0f;
// 			int[] neighborIndices = GetNeighborParticleIndices(particle.pos);
// 			for (int i2 = 0; i2 < neighborIndices.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
// 				int neighborIndex = neighborIndices[i2];
// 				float hasFoundNeighbor = Mathf.Clamp(Mathf.Round(neighborIndex + 1), 0, 1);
// 				if(neighborIndex == -1) break; // removable
// 				neighborIndex = Mathf.Max(0, neighborIndex);

// 				ElementParticle otherParticle = particles[neighborIndex];
// 				Vector2 dir = otherParticle.pos - particle.pos;
// 				float r2 = dir.sqrMagnitude * Mathf.Max(particle.GetRepelFactor(), otherParticle.GetRepelFactor());

// 				float temperature = particle.GetTemperature();
// 				float temperatureStartFrame = particle.GetTemperatureStartFrame();
// 				float temperatureOther = otherParticle.GetTemperature();
// 				float temperatureStartFrameOther = otherParticle.GetTemperatureStartFrame();

// 				float shouldApplyTemperature = Mathf.Ceil((Mathf.Clamp(Mathf.Sign(HSQ_TEMPERATURE - r2), 0, 1) + Mathf.Clamp(Mathf.Sign(temperatureStartFrame - temperatureStartFrameOther), 0, 1)) / 2.0f);
// 				//if (r2 < HSQ_TEMPERATURE && temperatureStartFrame > temperatureStartFrameOther){
// 					float diffTemperature = temperature - temperatureOther;
// 					float exchange = diffTemperature * particle.GetThermalDiffusivity();

// 					float newTemperature1 = temperature - exchange;
// 					float newTemperatureClamped1 = Mathf.Clamp(newTemperature1, 0, MAX_TEMPERATURE);

// 					float newTemperature2 = temperatureOther + exchange;
// 					float newTemperatureClamped2 = Mathf.Clamp(newTemperature2, 0, MAX_TEMPERATURE);
					
// 					float unusedTemperature = Mathf.Max(Mathf.Abs(newTemperatureClamped1 - newTemperature1), Mathf.Abs(newTemperature2 - newTemperatureClamped2));

// 					float temperatureDelta = hasFoundNeighbor * shouldApplyTemperature * (exchange - unusedTemperature);
// 					particle.SetTemperature(temperature - temperatureDelta);
// 					otherParticle.SetTemperature(temperatureOther + temperatureDelta);
// 					particles[neighborIndex] = otherParticle;
// 				//}
// 				float shouldApplyDensity = Mathf.Clamp(Mathf.Sign(HSQ - r2), 0, 1);
				
// //				if (r2 < HSQ){
// 					// this computation is symmetric
// 					particle.density += shouldApplyDensity * otherParticle.mass * POLY6 * Mathf.Pow(HSQ - r2, 3.0f);
// 				//				}
// 			}

// 			particle.pressure = GAS_CONST * (particle.density - REST_DENS);
// 			particles[i] = particle;
// 		}
// 	}

// 	void ComputeForces() {
// 		for (int i = 0; i < particles.Length; i++){
// 			ElementParticle particle = particles[i];
// 			Vector2 fpress = new Vector2();
// 			Vector2 fvisc = new Vector2();
// 			Vector2 fsurftens = new Vector2();
// 			int[] neighborIndices = GetNeighborParticleIndices(particle.pos);
// 			for (int i2 = 0; i2 < neighborIndices.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
// 				int neighborIndex = neighborIndices[i2];
// 				float hasFoundNeighbor = Mathf.Floor((Mathf.Clamp(neighborIndex + 1, 0, 1) + Mathf.Clamp(Mathf.Abs(neighborIndex - i), 0, 1)) / 2.0f);
// 				if(neighborIndex == -1) break; // removable
// 				if(neighborIndex == i) continue; // removable
// 				neighborIndex = Mathf.Max(0, neighborIndex);

// 				ElementParticle otherParticle = particles[neighborIndex];
// 				Vector2 diff = otherParticle.pos - particle.pos;
// 				float r = diff.magnitude * Mathf.Max(particle.GetRepelFactor(), otherParticle.GetRepelFactor());

// 				float isTouchingNeighbor = Mathf.Clamp(Mathf.Sign(H - r), 0, 1);
// 				//if (r < H){
// 					// compute pressure force contribution
// 					fpress += hasFoundNeighbor * isTouchingNeighbor * -diff.normalized * otherParticle.mass * (particle.pressure + otherParticle.pressure) / (2.0f * otherParticle.density) * SPIKY_GRAD * Mathf.Pow(H - r, 2.0f) * CLUSTERING_RESISTANCE;
// 					// compute viscosity force contribution
// 					fvisc += hasFoundNeighbor * isTouchingNeighbor * otherParticle.visc * otherParticle.mass * (otherParticle.velocity - particle.velocity) / otherParticle.density * VISC_LAP * (H - r);
					
// 					// bounce off frozen particles
// 					float shouldApplyBounce = Mathf.Floor((hasFoundNeighbor + isTouchingNeighbor + otherParticle.IsSolid()) / 3.0f);
// 					particle.velocity.x *= Mathf.Lerp(1.0f, diff.normalized.x * BOUND_DAMPING, shouldApplyBounce);
// 					particle.velocity.y *= Mathf.Lerp(1.0f, diff.normalized.y * BOUND_DAMPING, shouldApplyBounce);
// 				//}

// 				float shouldApplySurfaceTension = Mathf.Floor((Mathf.Clamp(Mathf.Sign(H_SURFACE_TENSION - r), 0, 1) + particle.IsLiquid()) / 2.0f);
// 				//if (r < H_SURFACE_TENSION && particle.IsLiquid() == 1.0f){
// 				// compute surface tension contribution
// 				fsurftens += shouldApplySurfaceTension * diff.normalized * VISC_LAP * SURFACE_TENSION_WATER;
// 				//}
// 			}

// 			Vector2 fgrav = G * particle.density;
// 			particle.force = fpress + fvisc + fgrav + fsurftens;

// 			particle.SetTemperatureStartFrame(particle.GetTemperature()); // for next frame
// 			particles[i] = particle;
// 		}
// 	}

//     void Integrate() {
// 		for (int i = 0; i < particles.Length; i++){
// 			ElementParticle particle = particles[i];

// 			// forward Euler integration
// 			particle.velocity += DT * particle.force / particle.density;
// 			particle.pos += DT * particle.velocity;


// 			// enforce boundary conditions
// 			int hasHitEdge = 0;
// 			if (particle.pos.x - EPS < 0.0f){
// 				particle.velocity.x *= BOUND_DAMPING;
// 				particle.pos.x = EPS;
// 				hasHitEdge = 1;
// 			}
// 			if (particle.pos.x + EPS > GRID_WIDTH_PIXELS){
// 				particle.velocity.x *= BOUND_DAMPING;
// 				particle.pos.x = GRID_WIDTH_PIXELS - EPS;
// 				hasHitEdge = 1;
// 			}
// 			if (particle.pos.y - EPS < 0.0f){
// 				particle.velocity.y *= BOUND_DAMPING;
// 				particle.pos.y = EPS;
// 				hasHitEdge = 1;
// 			}
// 			if (particle.pos.y + EPS > GRID_HEIGHT_PIXELS){
// 				particle.velocity.y *= BOUND_DAMPING;
// 				particle.pos.y = GRID_HEIGHT_PIXELS - EPS;
// 				hasHitEdge = 1;
// 			}

// 			float temperature = particle.GetTemperature();
// 			float diffTemperature = temperature - BOUND_TEMPERATURE;
// 			float isBoundaryWarmerThanParticle = Mathf.Clamp(Mathf.Sign(BOUND_TEMPERATURE - temperature), 0, 1);
// 			float exchange = diffTemperature;
// 			exchange *= Mathf.Lerp(particle.GetThermalDiffusivity(), BOUND_THERMAL_DIFFUSIVITY, isBoundaryWarmerThanParticle);
// 			particle.SetTemperature(temperature - exchange * hasHitEdge);

// 			particles[i] = particle;
// 		}
// 	}

// 	private const int BUCKET_EXTRA_EDGES = (int)((GRID_WIDTH_PIXELS * 2 + GRID_HEIGHT_PIXELS * 2) / H) / PARTICLE_BUCKET_SIZE;
// 	private const int BUCKET_COUNT = BUCKET_EXTRA_EDGES + (int)((GRID_WIDTH_PIXELS * GRID_HEIGHT_PIXELS) / H) / PARTICLE_BUCKET_SIZE;
// 	private const int PARTICLE_BUCKET_SIZE = 1;
// 	private const int PARTICLE_BUCKET_WIDTH = 1;
// 	private const int PARTICLE_BUCKET_COUNT_X = GRID_WIDTH_PIXELS / PARTICLE_BUCKET_WIDTH;
// 	private static Bucket[] particleBuckets = new Bucket[BUCKET_COUNT];
// 	class Bucket {
// 		public const int MAX_AMOUNT_OF_CONTENT = 64;
// 		private int[] content = new int[MAX_AMOUNT_OF_CONTENT];
// 		private int latestIndexAddedTo = -1;

// 		public void Clear() {
// 			latestIndexAddedTo = -1;
// 			for (int i = 0; i < MAX_AMOUNT_OF_CONTENT; i++){
// 				content[i] = -1;
// 			}
// 		}
// 		public void AddContent(int newContent) {
// 			latestIndexAddedTo++;
// 			if (latestIndexAddedTo >= MAX_AMOUNT_OF_CONTENT){ // should probably replace with list or something
// 				Debug.LogError("Bucket was overfilled!");
// 			}

// 			content[latestIndexAddedTo] = newContent;
// 		}
// 		public int[] GetContent() {
// 			return content;
// 		}
// 		public int GetContentAmount(){
// 			return latestIndexAddedTo + 1;
// 		}
// 	}

// 	void CacheParticlesInBuckets() {
// 		for (int i = 0; i < particleBuckets.Length; i++){
// 			particleBuckets[i].Clear();
// 		}
// 		for (int i = 0; i < particles.Length; i++){
// 			ElementParticle particle = particles[i];

// 			int index;
// 			int x, y;
// 			GetParticleBucketIndex(particle.pos, out index, out x, out y);
			
// 			particles[i].bucketIndex = index;
// 			particleBuckets[index].AddContent(i);
// 		}
// 	}

// 	private const int BUCKET_CLUSTER_SIZE = 9;
// 	private int[] bucketGridPosXs = new int[BUCKET_CLUSTER_SIZE];
// 	private int[] bucketGridPosYs = new int[BUCKET_CLUSTER_SIZE];
// 	private int[] bucketIndices = new int[BUCKET_CLUSTER_SIZE];
// 	private int[] bucketClusterContent = new int[BUCKET_CLUSTER_SIZE * Bucket.MAX_AMOUNT_OF_CONTENT];
// 	int[] GetNeighborParticleIndices(Vector2 pos) {
// 		int centerBucketIndex;
// 		int centerBucketX;
// 		int centerBucketY;
// 		GetParticleBucketIndex(pos, out centerBucketIndex, out centerBucketX, out centerBucketY);

// 		bucketIndices[0] = centerBucketIndex + PARTICLE_BUCKET_COUNT_X - 1;
// 		bucketIndices[1] = centerBucketIndex + PARTICLE_BUCKET_COUNT_X;
// 		bucketIndices[2] = centerBucketIndex + PARTICLE_BUCKET_COUNT_X + 1;
// 		bucketIndices[3] = centerBucketIndex - 1;
// 		bucketIndices[4] = centerBucketIndex;
// 		bucketIndices[5] = centerBucketIndex + 1;
// 		bucketIndices[6] = centerBucketIndex - PARTICLE_BUCKET_COUNT_X - 1;
// 		bucketIndices[7] = centerBucketIndex - PARTICLE_BUCKET_COUNT_X;
// 		bucketIndices[8] = centerBucketIndex - PARTICLE_BUCKET_COUNT_X + 1;

// 		bucketGridPosXs[0] = centerBucketX - 1;
// 		bucketGridPosYs[0] = centerBucketY + 1;

// 		bucketGridPosXs[1] = centerBucketX;
// 		bucketGridPosYs[1] = centerBucketY + 1;
		
// 		bucketGridPosXs[2] = centerBucketX + 1;
// 		bucketGridPosYs[2] = centerBucketY + 1;
		
// 		bucketGridPosXs[3] = centerBucketX - 1;
// 		bucketGridPosYs[3] = centerBucketY;
		
// 		bucketGridPosXs[4] = centerBucketX;
// 		bucketGridPosYs[4] = centerBucketY;
		
// 		bucketGridPosXs[5] = centerBucketX + 1;
// 		bucketGridPosYs[5] = centerBucketY;
		
// 		bucketGridPosXs[6] = centerBucketX - 1;
// 		bucketGridPosYs[6] = centerBucketY - 1;
		
// 		bucketGridPosXs[7] = centerBucketX;
// 		bucketGridPosYs[7] = centerBucketY - 1;
		
// 		bucketGridPosXs[8] = centerBucketX + 1;
// 		bucketGridPosYs[8] = centerBucketY - 1;

// 		int addedCount = 0;
// 		for (int clusterIndex = 0; clusterIndex < BUCKET_CLUSTER_SIZE; clusterIndex++){
// 			int bucketGridPosX = bucketGridPosXs[clusterIndex];
// 			int bucketGridPosY = bucketGridPosYs[clusterIndex];
// 			//bool isWithinGrid = bucketGridPosX >= 0 && bucketGridPosY >= 0 && bucketGridPosX < PARTICLE_BUCKET_COUNT_X && bucketGridPosY < PARTICLE_BUCKET_COUNT_X;
// 			int isWithinGrid = Mathf.FloorToInt((Mathf.Sign(bucketGridPosX) + Mathf.Sign(bucketGridPosY) + Mathf.Clamp(Mathf.Sign(PARTICLE_BUCKET_COUNT_X - 1 - bucketGridPosX), 0, 1) + Mathf.Clamp(Mathf.Sign(PARTICLE_BUCKET_COUNT_X - 1 - bucketGridPosY), 0, 1)) / 4.0f);
// 			//if(!isWithinGrid) continue;

// 			int bucketIndex = Mathf.Clamp(bucketIndices[clusterIndex], 0, particleBuckets.Length);
// 			int[] bucketContent = particleBuckets[bucketIndex].GetContent();
// 			for (int contentIndex = 0; contentIndex < bucketContent.Length; contentIndex++){
// 				int content = bucketContent[contentIndex];
// 				int hasFoundContent = (int)Mathf.Clamp(Mathf.Sign(content), 0, 1);
// 				//if(content == -1) break;
// 				int shouldAddContent = Mathf.FloorToInt((isWithinGrid + hasFoundContent) / 2.0f);
// 				bucketClusterContent[addedCount] = (int)Mathf.Lerp(bucketClusterContent[addedCount], content, shouldAddContent);
// 				addedCount += shouldAddContent;
// 			}
// 		}
// 		for (int i = addedCount; i < bucketClusterContent.Length; i++){
// 			bucketClusterContent[i] = -1;
// 		}

// 		return bucketClusterContent;
// 	}

// 	void GetParticleBucketIndex(Vector2 pos, out int index, out int x, out int y) {
// 		float bucketWidth = (float)PARTICLE_BUCKET_WIDTH;
// 		x = Mathf.FloorToInt(pos.x / bucketWidth) + 1; // +1 to account for extra edges
// 		y = Mathf.FloorToInt(pos.y / bucketWidth) + 1; // +1 to account for extra edges
// 		index = y * PARTICLE_BUCKET_COUNT_X + x;
// 	}
#endregion
}