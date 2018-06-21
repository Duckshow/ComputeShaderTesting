using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementSimulator : MonoBehaviour {

	struct ElementParticle{
		public Vector2 pos;
		public Vector2 velocity; 
		public Vector2 force;
		public float density;
		public float pressure;

		public ElementParticle(float x, float y) {
			pos = new Vector2(x, y);
			velocity = new Vector2();
			force = new Vector2();
			density = 0;
			pressure = 0;
		}

		// public static int GetStride() {
		// 	return sizeof(float) * 14;
		// }
	};

	// solver parameters
	private static readonly Vector2 G = new Vector2(0, 12000 * -9.8f); // external (gravitational) forces
	private const float REST_DENS = 1000.0f; // rest density
	private const float GAS_CONST = 2000.0f; // const for equation of state
	private const float H = 16.0f; // interaction radius
	private const float HSQ = H * H; // radius^2 for optimization
	private const float MASS = 65.0f; // assume all particles have the same mass
	private const float VISC = 250.0f; // viscosity constant
	private const float DT = 0.0008f; // integration timestep

	// smoothing kernels defined in Müller and their gradients
	private static readonly float POLY6 = 315.0f / (65.0f * Mathf.PI * Mathf.Pow(H, 9.0f));
	private static readonly float SPIKY_GRAD = -45.0f / (Mathf.PI * Mathf.Pow(H, 6.0f));
	private static readonly float VISC_LAP = 45.0f / (Mathf.PI * Mathf.Pow(H, 6.0f));

	// simulation parameters
	private const float EPS = H; // boundary epsilon
	private const float BOUND_DAMPING = -0.5f;
	private const int DAM_PARTICLES = 1000;
	private const int MAX_PARTICLES = 500;
	private const int BLOCK_PARTICLES = 1000;

	// ============== WARNING: shared with ElementEmulator.compute! must be equal!
	private const int PIXELS_PER_TILE_EDGE = 256;
	private const int GRID_WIDTH_TILES = 4;
	private const int GRID_HEIGHT_TILES = 3;
	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
	//===============

	private const int THREAD_COUNT_MAX = 1024;


	private ElementParticle[] particles;

	[SerializeField]
	private ComputeShader shader;
	[SerializeField]
	private Material material;
    [SerializeField]
    private ParticleSystem particleSystem;

	// private int threadCountAxis;
	// private int threadGroupCountX;
	// private int threadGroupCountY;

	// private ComputeBuffer bufferElementParticles;
	// private RenderTexture output;

	// private const string KERNEL_INTEGRATE = "Integrate";
	// private int kernelID_Integrate;

	// private const string KERNEL_COMPUTE_FORCES = "ComputeForces";
	// private int kernelID_ComputeForces;

	// private const string KERNEL_COMPUTE_DENSITY_AND_PRESSURE = "ComputeDensityAndPressure";
	// private int kernelID_ComputeDensityAndPressure;

	// private const string KERNEL_RENDER_TO_TEXTURE = "RenderToTexture";
	// private int kernelID_RenderToTexture;

	// private const string PROPERTY_ELEMENT_PARTICLES = "elementParticles";
	// private int shaderPropertyID_elementParticles;

	// private const string PROPERTY_OUTPUT = "output";
	// private int shaderPropertyID_output;


	void Awake() {
		// kernelID_Integrate = shader.FindKernel(KERNEL_INTEGRATE);
		// kernelID_ComputeForces = shader.FindKernel(KERNEL_COMPUTE_FORCES);
		// kernelID_ComputeDensityAndPressure = shader.FindKernel(KERNEL_COMPUTE_DENSITY_AND_PRESSURE);
		// kernelID_RenderToTexture = shader.FindKernel(KERNEL_RENDER_TO_TEXTURE);

		// threadCountAxis = (int)Mathf.Sqrt(THREAD_COUNT_MAX);
		// threadGroupCountX = GRID_WIDTH_PIXELS / threadCountAxis;
		// threadGroupCountY = GRID_HEIGHT_PIXELS / threadCountAxis;

		// shaderPropertyID_elementParticles = Shader.PropertyToID(PROPERTY_ELEMENT_PARTICLES);
		// shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
	}

	void OnDisable(){
		//bufferElementParticles.Dispose();
	}

	void Start () {
		InitSPH();
		InitShader();
	}

	void InitShader() { 
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		// output = new RenderTexture(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS, 24);
		// output.enableRandomWrite = true;
		// output.filterMode = FilterMode.Point;
		// output.Create();

		// bufferElementParticles = new ComputeBuffer(particles.Length, ElementParticle.GetStride());
	}

	void InitSPH(){
		List<ElementParticle> startParticles = new List<ElementParticle>();

		for (float y = EPS; y < GRID_HEIGHT_PIXELS - EPS * 2.0f; y += H) {
			for (float x = EPS; x < GRID_WIDTH_PIXELS * 0.5f - EPS * 2.0f; x += H) {
				if (startParticles.Count < Mathf.Min(DAM_PARTICLES, MAX_PARTICLES)){
                   // if(Random.value < (x / GRID_WIDTH_PIXELS) * 4) continue;
					float jitter = Random.value / H;
					startParticles.Add(new ElementParticle(x + jitter, y));
				}
			}
		}

		particles = startParticles.ToArray();
	}

	void FixedUpdate () {
		GetInput();

		// bufferElementParticles.SetData(particles);
		// shader.SetBuffer(kernelID_ComputeDensityAndPressure, PROPERTY_ELEMENT_PARTICLES, bufferElementParticles);
		// shader.Dispatch(kernelID_ComputeDensityAndPressure, threadGroupCountX, threadGroupCountY, 1);
		// bufferElementParticles.GetData(particles);

		// bufferElementParticles.SetData(particles);
		// shader.SetBuffer(kernelID_ComputeForces, PROPERTY_ELEMENT_PARTICLES, bufferElementParticles);
		// shader.Dispatch(kernelID_ComputeForces, threadGroupCountX, threadGroupCountY, 1);
		// bufferElementParticles.GetData(particles);

		// bufferElementParticles.SetData(particles);
		// shader.SetBuffer(kernelID_Integrate, PROPERTY_ELEMENT_PARTICLES, bufferElementParticles);
		// shader.Dispatch(kernelID_Integrate, threadGroupCountX, threadGroupCountY, 1);
		// bufferElementParticles.GetData(particles);

		// bufferElementParticles.SetData(particles);
		// shader.SetBuffer(kernelID_RenderToTexture, PROPERTY_ELEMENT_PARTICLES, bufferElementParticles);
		// shader.SetTexture(kernelID_RenderToTexture, PROPERTY_OUTPUT, output);
		// shader.Dispatch(kernelID_RenderToTexture, threadGroupCountX, threadGroupCountY, 1);
		// bufferElementParticles.GetData(particles);

		//material.mainTexture = output;

		ComputeDensityAndPressure();
		ComputeForces();
		Integrate();

        particleSystem.Clear();
        ParticleSystem.MainModule main = particleSystem.main;
        main.startSize = H * 2;
        particleSystem.Emit(particles.Length);

        ParticleSystem.Particle[] emittedParticles = new ParticleSystem.Particle[particleSystem.particleCount];
        particleSystem.GetParticles(emittedParticles);
        for (int i = 0; i < particleSystem.particleCount; i++){
            emittedParticles[i].position = particles[i].pos;
        }
        particleSystem.SetParticles(emittedParticles, particleSystem.particleCount);
	}
	
	void ComputeDensityAndPressure() {
		for (int i = 0; i < particles.Length; i++){
			ElementParticle particle = particles[i];

			particle.density = 0.0f;
			for (int i2 = 0; i2 < particles.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
				ElementParticle otherParticle = particles[i2];
				Vector2 dir = otherParticle.pos - particle.pos;
				float r2 = dir.sqrMagnitude;

				if (r2 < HSQ){
					// this computation is symmetric
					particle.density += MASS * POLY6 * Mathf.Pow(HSQ - r2, 3.0f);
				}
			}

			particle.pressure = GAS_CONST * (particle.density - REST_DENS);
			particles[i] = particle;
		}
	}
	
	void ComputeForces() {
		for (int i = 0; i < particles.Length; i++){
			ElementParticle particle = particles[i];
			Vector2 fpress = new Vector2();
			Vector2 fvisc = new Vector2();
			for (int i2 = 0; i2 < particles.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
				ElementParticle otherParticle = particles[i2];
				if(i == i2) continue;

				Vector2 diff = otherParticle.pos - particle.pos;
				float r = diff.magnitude;

				if (r < H){
					Vector2 antiDir = -diff.normalized;
					if (antiDir.x == 0 && antiDir.y == 0){
					 	antiDir.x = -Random.value;
						antiDir.y = -Random.value;
					}

					// compute pressure force contribution
					fpress += antiDir * MASS * (particle.pressure + otherParticle.pressure) / (2.0f * otherParticle.density) * SPIKY_GRAD * Mathf.Pow(H - r, 2.0f);
					// compute viscosity force contribution
					fvisc += VISC * MASS * (otherParticle.velocity - particle.velocity) / otherParticle.density * VISC_LAP * (H - r);
				}
			}

			Vector2 fgrav = G * particle.density;
			particle.force = fpress + fvisc + fgrav;
			particles[i] = particle;
		}
	}

    void Integrate() {
		for (int i = 0; i < particles.Length; i++){
			ElementParticle particle = particles[i];

			// forward Euler integration
			// if (i == 0){
			// 	Debug.Log(particle.force / particle.density * 0.2f);
			// }
			particle.velocity += DT * particle.force / particle.density;
			particle.pos += DT * particle.velocity;

			// enforce boundary conditions
			if (particle.pos.x - EPS < 0.0f){
				particle.velocity.x *= BOUND_DAMPING;
				particle.pos.x = EPS;
			}
			if (particle.pos.x + EPS > GRID_WIDTH_PIXELS){
				particle.velocity.x *= BOUND_DAMPING;
				particle.pos.x = GRID_WIDTH_PIXELS - EPS;
			}
			if (particle.pos.y - EPS < 0.0f){
				particle.velocity.y *= BOUND_DAMPING;
				particle.pos.y = EPS;
			}
			if (particle.pos.y + EPS > GRID_HEIGHT_PIXELS){
				particle.velocity.y *= BOUND_DAMPING;
				particle.pos.y = GRID_HEIGHT_PIXELS - EPS;
			}

			particles[i] = particle;
		}
	}

	void GetInput(){
		if (Input.GetKey(KeyCode.Space)){
			if (particles.Length >= MAX_PARTICLES) { 
				Debug.LogWarning("Maximum amount reached!");
			}
			else{
				List<ElementParticle> newParticles = new List<ElementParticle>(particles);
				int placed = 0;
				for (float y = GRID_HEIGHT_PIXELS / 1.5f - GRID_HEIGHT_PIXELS / 5.0f; y < GRID_HEIGHT_PIXELS / 1.5f + GRID_HEIGHT_PIXELS / 5.0f; y += H * 0.95f){
					for (float x = GRID_WIDTH_PIXELS / 2.0f - GRID_HEIGHT_PIXELS / 5.0f; x <= GRID_WIDTH_PIXELS / 2.0f + GRID_HEIGHT_PIXELS / 5.0f; x += H * 0.95f){
						placed++;
						if (placed < BLOCK_PARTICLES && newParticles.Count < MAX_PARTICLES){
							newParticles.Add(new ElementParticle(x, y));
						}
					}
				}
                ElementParticle[] newParticleArray = new ElementParticle[particles.Length + newParticles.Count];
                int index = 0;
                for (int i = 0; i < particles.Length; i++){
                    newParticleArray[index] = particles[i];
                }
                for (int i = 0; i < newParticles.Count; i++){
                    newParticleArray[index] = newParticles[i];
                }
                particles = newParticleArray;
			}
		}
		if (Input.GetKeyUp(KeyCode.R)){
			particles = null;
			InitSPH();
		}
	}
}
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class ElementSimulator : MonoBehaviour
// {

// 	private static int highestAliveParticleIndex = -1;

// 	struct ElementParticle
// 	{
// 		public Vector2 pos;
// 		public Vector2 velocity;
// 		public Vector2 force;
// 		public float density;
// 		public float pressure;
// 		public float isAlive;

// 		public static int GetStride()
// 		{
// 			return sizeof(float) * 9;
// 		}

// 		public void SetPosition(int particleIndex, float x, float y)
// 		{
// 			pos.Set(x, y);
// 			isAlive = 1.0f;

// 			if (particleIndex > ElementSimulator.highestAliveParticleIndex)
// 			{
// 				ElementSimulator.highestAliveParticleIndex = particleIndex;
// 			}
// 		}
// 	};

// 	// ============== WARNING: shared with ElementSimulator.compute! must be equal!
// 	private const int PIXELS_PER_TILE_EDGE = 32;
// 	private const int GRID_WIDTH_TILES = 3;
// 	private const int GRID_HEIGHT_TILES = 1;
// 	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
// 	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
// 	private const float H = 16.0f; // kernel radius
// 	private const float EPS = H; // boundary epsilon
// 								 //===============

// 	private const int THREAD_COUNT_MAX = 1024;

// 	private const int DAM_PARTICLES_X = 10;
// 	private const int DAM_PARTICLES_Y = 10;
// 	private const int MAX_PARTICLES = 10000;
// 	private const int BLOCK_PARTICLES = 500;


// 	private ElementParticle[] particles;

// 	[SerializeField]
// 	private ComputeShader shader;
// 	[SerializeField]
// 	private Material material;

// 	private int threadGroupCountX;
// 	private int threadGroupCountY;

// 	private ComputeBuffer bufferParticles;
// 	private RenderTexture output;

// 	private const string KERNEL_INTEGRATE = "Integrate";
// 	private int kernelID_Integrate;

// 	private const string KERNEL_COMPUTE_FORCES = "ComputeForces";
// 	private int kernelID_ComputeForces;

// 	private const string KERNEL_COMPUTE_DENSITY_AND_PRESSURE = "ComputeDensityAndPressure";
// 	private int kernelID_ComputeDensityAndPressure;

// 	private const string KERNEL_RENDER_TO_TEXTURE = "RenderToTexture";
// 	private int kernelID_RenderToTexture;

// 	private const string PROPERTY_PARTICLE_COUNT = "particleCount";
// 	private int shaderPropertyID_particleCount;

// 	private const string PROPERTY_PARTICLES = "particles";
// 	private int shaderPropertyID_particles;

// 	private const string PROPERTY_OUTPUT = "output";
// 	private int shaderPropertyID_output;



// 	void Awake()
// 	{
// 		kernelID_Integrate = shader.FindKernel(KERNEL_INTEGRATE);
// 		kernelID_ComputeForces = shader.FindKernel(KERNEL_COMPUTE_FORCES);
// 		kernelID_ComputeDensityAndPressure = shader.FindKernel(KERNEL_COMPUTE_DENSITY_AND_PRESSURE);
// 		kernelID_RenderToTexture = shader.FindKernel(KERNEL_RENDER_TO_TEXTURE);

// 		threadGroupCountX = GRID_WIDTH_PIXELS / 64;
// 		threadGroupCountY = 1;

// 		shaderPropertyID_particleCount = Shader.PropertyToID(PROPERTY_PARTICLE_COUNT);
// 		shaderPropertyID_particles = Shader.PropertyToID(PROPERTY_PARTICLES);
// 		shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
// 	}

// 	void OnDisable()
// 	{
// 		bufferParticles.Dispose();
// 	}

// 	void Start()
// 	{
// 		InitSPH();
// 		InitShader();
// 	}

// 	void InitShader()
// 	{
// 		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

// 		// output = new RenderTexture(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS, 24);
// 		// output.enableRandomWrite = true;
// 		// output.filterMode = FilterMode.Point;
// 		// output.Create();

// 		bufferParticles = new ComputeBuffer(particles.Length, ElementParticle.GetStride());
// 	}

// 	void InitSPH()
// 	{
// 		// List<ElementParticle> startParticles = new List<ElementParticle>();

// 		// for (float y = EPS; y < GRID_HEIGHT_PIXELS - EPS * 2.0f; y += H) {
// 		// 	for (float x = GRID_WIDTH_PIXELS / 4.0f; x <= GRID_WIDTH_PIXELS / 2.0f; x += H) {
// 		// 		if (startParticles.Count < DAM_PARTICLES){
// 		// 			float jitter = Random.value;
// 		// 			startParticles.Add(new ElementParticle(x + jitter, y));
// 		// 		}
// 		// 	}
// 		// }
// 		//particles = startParticles.ToArray();

// 		particles = new ElementParticle[MAX_PARTICLES];
// 		int i = 0;
// 		for (int y = 0; y < DAM_PARTICLES_Y; y++)
// 		{
// 			for (int x = 0; x < DAM_PARTICLES_X; x++)
// 			{
// 				particles[i].SetPosition(i, x, y);
// 				i++;
// 			}
// 		}
// 	}

// 	void Update()
// 	{
// 		//GetInput();

// 		bufferParticles.SetData(particles);
// 		shader.SetBuffer(kernelID_ComputeDensityAndPressure, shaderPropertyID_particles, bufferParticles);
// 		shader.SetFloat(shaderPropertyID_particleCount, particles.Length);
// 		shader.Dispatch(kernelID_ComputeDensityAndPressure, threadGroupCountX, threadGroupCountY, 1);
// 		bufferParticles.GetData(particles);

// 		bufferParticles.SetData(particles);
// 		shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_particles, bufferParticles);
// 		shader.SetFloat(shaderPropertyID_particleCount, particles.Length);
// 		shader.Dispatch(kernelID_ComputeForces, threadGroupCountX, threadGroupCountY, 1);
// 		bufferParticles.GetData(particles);

// 		bufferParticles.SetData(particles);
// 		shader.SetBuffer(kernelID_Integrate, shaderPropertyID_particles, bufferParticles);
// 		shader.SetFloat(shaderPropertyID_particleCount, particles.Length);
// 		shader.Dispatch(kernelID_Integrate, threadGroupCountX, threadGroupCountY, 1);
// 		bufferParticles.GetData(particles);

// 		bufferParticles.SetData(particles);
// 		shader.SetBuffer(kernelID_RenderToTexture, shaderPropertyID_particles, bufferParticles);
// 		shader.SetFloat(shaderPropertyID_particleCount, particles.Length);
// 		output = new RenderTexture(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS, 24);
// 		output.enableRandomWrite = true;
// 		output.filterMode = FilterMode.Point;
// 		output.Create();
// 		shader.SetTexture(kernelID_RenderToTexture, shaderPropertyID_output, output);
// 		shader.Dispatch(kernelID_RenderToTexture, threadGroupCountX, threadGroupCountY, 1);
// 		bufferParticles.GetData(particles);

// 		material.mainTexture = output;
// 	}

// 	void GetInput()
// 	{
// 		if (Input.GetKey(KeyCode.Space))
// 		{
// 			int placed = 0;
// 			int initiallyHighestAliveIndex = highestAliveParticleIndex;
// 			for (float y = GRID_HEIGHT_PIXELS / 1.5f - GRID_HEIGHT_PIXELS / 5.0f; y < GRID_HEIGHT_PIXELS / 1.5f + GRID_HEIGHT_PIXELS / 5.0f; y += H * 0.95f)
// 			{
// 				for (float x = GRID_WIDTH_PIXELS / 2.0f - GRID_HEIGHT_PIXELS / 5.0f; x <= GRID_WIDTH_PIXELS / 2.0f + GRID_HEIGHT_PIXELS / 5.0f; x += H * 0.95f)
// 				{
// 					placed++;
// 					if (placed < BLOCK_PARTICLES && initiallyHighestAliveIndex + placed < MAX_PARTICLES)
// 					{
// 						particles[highestAliveParticleIndex + 1].SetPosition(initiallyHighestAliveIndex + placed - 1, x, y);
// 					}
// 				}
// 			}
// 		}
// 		if (Input.GetKeyUp(KeyCode.R))
// 		{
// 			particles = null;
// 			InitSPH();
// 		}
// 	}
// }
