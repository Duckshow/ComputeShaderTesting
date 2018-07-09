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
		public float mass;
		public float visc;
		public float temperature;
		public float temperatureOld;
		public int elementIndex;
		public int bucketIndex;

		public ElementParticle(int elementIndex, float x, float y, float mass, float visc, float temperature) {
			pos = new Vector2(x, y);
			velocity = new Vector2();
			force = new Vector2();
			density = 0.0f;
			pressure = 0.0f;
			this.mass = mass;
			this.visc = visc;
			this.temperature = temperature;
			this.temperatureOld = temperature;
			this.elementIndex = elementIndex;
			bucketIndex = -1;
		}

		// public static int GetStride() {
		// 	return sizeof(float) * 14;
		// }
	};

	// solver parameters
	private static readonly Vector2 G = new Vector2(0, 600 * -9.8f); // external (gravitational) forces
	private const float REST_DENS = 1000.0f; // rest density
	private const float GAS_CONST = 2000.0f; // const for equation of state
	private const float H = 0.5f; // interaction radius
	private const float HSQ = H * H; // radius^2 for optimization
	private const float MASS = 2.0f; // assume all particles have the same mass
	private const float VISC = 250.0f; // viscosity constant
	private const float DT = 0.0008f; // integration timestep
	private const float DENSITY_OFFSET = 0.925f; // make SPIKY_GRAD apply force earlier (particles don't have to be as close)
	private const float VISC_OFFSET = 0.925f; // make VISC_LAP apply force earlier (particles don't have to be as close)

	// smoothing kernels defined in Müller and their gradients
	private static readonly float POLY6 = 315.0f / (65.0f * Mathf.PI * Mathf.Pow(H, 9.0f));
	private static readonly float SPIKY_GRAD = -45.0f / (Mathf.PI * Mathf.Pow(H * DENSITY_OFFSET, 6.0f));
	private static readonly float VISC_LAP = 45.0f / (Mathf.PI * Mathf.Pow(H * VISC_OFFSET, 6.0f));

	// simulation parameters
	private const float EPS = H; // boundary epsilon
	private const float BOUND_DAMPING = -0.5f;
	private const int DAM_PARTICLES_L = 250;
	private const int DAM_PARTICLES_R = 250;
	private const int MAX_PARTICLES = 2000;

	// ============== WARNING: shared with ElementEmulator.compute! must be equal!
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 2;
	private const int GRID_HEIGHT_TILES = 1;
	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
	//===============

	private const float MIN_TIME_BETWEEN_PARTICLE_SPAWN = 0.1f;
	private const float MAX_TEMPERATURE = 1000.0f;

	private const float THERMAL_DIFFUSIVITY = 500.0f;

	private ElementParticle[] particles;

	[SerializeField]
	private ComputeShader shader;
	[SerializeField]
	private Material material;
    [SerializeField]
    private ParticleSystem particleSystem;
	[SerializeField]
	private float repelStrength = 1.0f;
	[SerializeField]
	private float mass0 = 1.0f;
	[SerializeField]
	private float mass1 = 1.0f;
	[SerializeField]
	private float visc0 = 1.0f;
	[SerializeField]
	private float visc1 = 1.0f;

	private float repelFactor;
	private List<ElementParticle> startParticles;
	private ParticleSystem.Particle[] emittedParticles;
	private float nextTimeSpawnIsAllowed = 0.0f;



	void Start () {
		InitSPH();
		InitShader();
	}

	void InitShader() { 
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);
	}

	void InitSPH(){
		repelFactor = 1 / repelStrength;
		mass0 *= MASS;
		mass1 *= MASS;
		visc0 *= VISC;
		visc1 *= VISC;

		for (int i = 0; i < particleBuckets.Length; i++){
			particleBuckets[i] = new Bucket();
		}

		startParticles = new List<ElementParticle>();

		int addedCount = 0;
		for (float x = EPS; x < GRID_WIDTH_PIXELS * 0.5f - EPS * 2.0f; x += H) {
			for (float y = EPS; y < GRID_HEIGHT_PIXELS - EPS * 2.0f; y += H) {
				if (addedCount < Mathf.Min(DAM_PARTICLES_L, MAX_PARTICLES)){
					float jitter = Random.value / H;
					startParticles.Add(new ElementParticle(0, x + jitter, y, mass0, visc0, MAX_TEMPERATURE));
					addedCount++;
				}
			}
		}

		addedCount = 0;
		for (float x = GRID_WIDTH_PIXELS - EPS * 4.0f; x > EPS; x -= H) {
			for (float y = GRID_HEIGHT_PIXELS - EPS * 4.0f; y > EPS; y -= H) {
				if (addedCount < Mathf.Min(DAM_PARTICLES_R, MAX_PARTICLES)){
					float jitter = Random.value / H;
					startParticles.Add(new ElementParticle(1, x + jitter, y, mass1, visc1, 0.0f));
					addedCount++;
				}
			}
		}

		particles = startParticles.ToArray();
	}

	void Update(){
		if (particles.Length == MAX_PARTICLES) {
			return;
		}
		if (Time.time < nextTimeSpawnIsAllowed){
			return;
		}
		nextTimeSpawnIsAllowed = Time.time + MIN_TIME_BETWEEN_PARTICLE_SPAWN;

		float mousePosX;
		float mousePosY;
		GetMousePositionOnGrid(out mousePosX, out mousePosY);

		if (mousePosX < 0 || mousePosY < 0 || mousePosX >= transform.position.x + GRID_WIDTH_PIXELS || mousePosY >= transform.position.y + GRID_HEIGHT_PIXELS){
			return;
		}

		if (Input.GetKey(KeyCode.Mouse0)){
			startParticles = new List<ElementParticle>(particles);
			ElementParticle particle = new ElementParticle(0, mousePosX, mousePosY, mass0, visc0, MAX_TEMPERATURE);
			particle.velocity.x = 1000.0f;
			startParticles.Add(particle);
			particles = startParticles.ToArray();
		}

		if (Input.GetKey(KeyCode.Mouse1)){
			startParticles = new List<ElementParticle>(particles);
			ElementParticle particle = new ElementParticle(1, mousePosX, mousePosY, mass1, visc1, 0.0f);
			particle.velocity.x = -1000.0f;
			startParticles.Add(particle);
			particles = startParticles.ToArray();
		}
	}

	void GetMousePositionOnGrid(out float x, out float y){
		Vector2 gridPosStart = Camera.main.WorldToScreenPoint(transform.position);
		Vector2 gridPosEnd = Camera.main.WorldToScreenPoint(transform.position + new Vector3(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS));
		float mousePosX01 = (Input.mousePosition.x - gridPosStart.x) / gridPosEnd.x;
		float mousePosY01 = (Input.mousePosition.y - gridPosStart.y) / gridPosEnd.y;
		x = GRID_WIDTH_PIXELS * mousePosX01;
		y = GRID_HEIGHT_PIXELS * mousePosY01;
	}

	void FixedUpdate () {
		CacheParticlesInBuckets();
		ComputeDensityAndPressure();
		ComputeTemperatureAndPressure();
		ComputeForces();
		Integrate();
		// for (int i = 0; i < particles.Length; i++){
		// 	int[] neighbors = GetNeighborParticleIndices(particles[i].pos);
		// }

        particleSystem.Clear();
        ParticleSystem.MainModule main = particleSystem.main;
        main.startSize = H * 2;
        particleSystem.Emit(particles.Length);

		if (emittedParticles == null || emittedParticles.Length != particleSystem.particleCount){
			emittedParticles = new ParticleSystem.Particle[particleSystem.particleCount];
		}
        particleSystem.GetParticles(emittedParticles);
        for (int i = 0; i < particleSystem.particleCount; i++){
            emittedParticles[i].position = particles[i].pos;
			if (i == 0){
//				Debug.Log(particles[i].temperature);
				emittedParticles[i].startColor = Color.cyan;
			}
			else{
				emittedParticles[i].startColor = Color.Lerp(Color.blue, Color.red, particles[i].temperature / MAX_TEMPERATURE);
				//emittedParticles[i].startColor = new Color(particles[i].elementIndex, 0, 1 - particles[i].elementIndex, 1);
				//emittedParticles[i].startColor = new Color((Mathf.Clamp((float)particleBuckets[particles[i].bucketIndex].GetContentAmount() * 2 / 255.0f, 0, 1)), 0, 0, 1);
			}
		}
        particleSystem.SetParticles(emittedParticles, particleSystem.particleCount);

		// for (int i = 0; i < particleBuckets.Length; i++){
		// 	int length = particleBuckets[i].GetContentAmount();
		// 	if(length > 0) Debug.Log(length);
		// }
		//Debug.Log(HYPOTHETICAL_MAX_AMOUNT_PARTICLES + ", " + PARTICLE_BUCKET_COUNT);

		//Debug.LogError("");

	}
	
	void ComputeDensityAndPressure() {
		for (int i = 0; i < particles.Length; i++){
			ElementParticle particle = particles[i];

			particle.density = 0.0f;
			int[] neighborIndices = GetNeighborParticleIndices(particle.pos);
			for (int i2 = 0; i2 < neighborIndices.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
				int neighborIndex = neighborIndices[i2];
				if(neighborIndex == -1) break;

				ElementParticle otherParticle = particles[neighborIndex];
				Vector2 dir = otherParticle.pos - particle.pos;
				float r2 = dir.sqrMagnitude * repelFactor;

				if (r2 < HSQ){
					// this computation is symmetric
					particle.density += otherParticle.mass * POLY6 * Mathf.Pow(HSQ - r2, 3.0f);
					particle.temperatureOld = particle.temperature;
				}
			}

			//particle.pressure = GAS_CONST * (particle.density - REST_DENS);
			particles[i] = particle;
		}
	}

	void ComputeTemperatureAndPressure() { 
		for (int i = 0; i < particles.Length; i++){
			ElementParticle particle = particles[i];

			int[] neighborIndices = GetNeighborParticleIndices(particle.pos);
			for (int i2 = 0; i2 < neighborIndices.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
				int neighborIndex = neighborIndices[i2];
				if(neighborIndex == -1) break;

				ElementParticle otherParticle = particles[neighborIndex];
				Vector2 dir = otherParticle.pos - particle.pos;
				float r2 = dir.sqrMagnitude * repelFactor;

				if (r2 < HSQ){
					float diffTemperature = particle.temperatureOld - otherParticle.temperatureOld;
					float exchange = 0.5f * THERMAL_DIFFUSIVITY * DT * diffTemperature;

					float min = Mathf.Min(particle.temperatureOld, otherParticle.temperatureOld);
					float max = Mathf.Max(MAX_TEMPERATURE - particle.temperatureOld, MAX_TEMPERATURE - otherParticle.temperatureOld);
					float min2 = Mathf.Min(min, max);
					float max2 = Mathf.Max(min, max);

					exchange = Mathf.Clamp(exchange, min2, max2);

					//particle.density -= exchange;
					particle.temperature -= exchange;
					//otherParticle.density += exchange;
					otherParticle.temperature += exchange;
					if (i == 0){
						Debug.Log(THERMAL_DIFFUSIVITY + " * " + DT + " * " + diffTemperature + " (" + particle.temperatureOld + ", " + otherParticle.temperatureOld + ")");
					}

					particles[neighborIndex] = otherParticle;
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
			int[] neighborIndices = GetNeighborParticleIndices(particle.pos);
			for (int i2 = 0; i2 < neighborIndices.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
				int neighborIndex = neighborIndices[i2];
				if(neighborIndex == -1) break;
				if(neighborIndex == i) continue;

				ElementParticle otherParticle = particles[neighborIndex];
				Vector2 diff = otherParticle.pos - particle.pos;
				float r = diff.magnitude * repelFactor;

				if (r < H){
					// compute pressure force contribution
					fpress += -diff.normalized * otherParticle.mass * (particle.pressure + otherParticle.pressure) / (2.0f * otherParticle.density) * SPIKY_GRAD * Mathf.Pow(H - r, 2.0f);
					// compute viscosity force contribution
					fvisc += otherParticle.visc * otherParticle.mass * (otherParticle.velocity - particle.velocity) / otherParticle.density * VISC_LAP * (H - r);
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

	private const int HYPOTHETICAL_MAX_AMOUNT_PARTICLES = (int)((GRID_WIDTH_PIXELS * GRID_HEIGHT_PIXELS) / H);
	private const int PARTICLE_BUCKET_SIZE = 1;
	private const int PARTICLE_BUCKET_WIDTH = 1;
	private const int PARTICLE_BUCKET_COUNT = HYPOTHETICAL_MAX_AMOUNT_PARTICLES / PARTICLE_BUCKET_SIZE;
	private const int PARTICLE_BUCKET_COUNT_X = GRID_WIDTH_PIXELS / PARTICLE_BUCKET_WIDTH;
	private static Bucket[] particleBuckets = new Bucket[HYPOTHETICAL_MAX_AMOUNT_PARTICLES / PARTICLE_BUCKET_SIZE];
	class Bucket {
		public const int MAX_AMOUNT_OF_CONTENT = 32;
		private int[] content = new int[MAX_AMOUNT_OF_CONTENT];
		private int latestIndexAddedTo = -1;

		public void Clear() {
			latestIndexAddedTo = -1;
			for (int i = 0; i < MAX_AMOUNT_OF_CONTENT; i++){
				content[i] = -1;
			}
		}
		public void AddContent(int newContent) {
			latestIndexAddedTo++;
			if (latestIndexAddedTo >= MAX_AMOUNT_OF_CONTENT){
				Debug.LogError("Bucket was overfilled!");
			}

			content[latestIndexAddedTo] = newContent;
		}
		public int[] GetContent() {
			return content;
		}
		public int GetContentAmount(){
			return latestIndexAddedTo + 1;
		}
	}

	void CacheParticlesInBuckets() {
		for (int i = 0; i < particleBuckets.Length; i++){
			particleBuckets[i].Clear();
		}
		for (int i = 0; i < particles.Length; i++){
			ElementParticle particle = particles[i];

			int index;
			int x, y;
			GetParticleBucketIndex(particle.pos, out index, out x, out y);
			
			particles[i].bucketIndex = index;
			particleBuckets[index].AddContent(i);
		}
	}

	private const int BUCKET_CLUSTER_SIZE = 9;
	private int[] bucketGridPosXs = new int[BUCKET_CLUSTER_SIZE];
	private int[] bucketGridPosYs = new int[BUCKET_CLUSTER_SIZE];
	private int[] bucketIndices = new int[BUCKET_CLUSTER_SIZE];
	private int[] bucketClusterContent = new int[BUCKET_CLUSTER_SIZE * Bucket.MAX_AMOUNT_OF_CONTENT];
	int[] GetNeighborParticleIndices(Vector2 pos) {
		int centerBucketIndex;
		int centerBucketX;
		int centerBucketY;
		GetParticleBucketIndex(pos, out centerBucketIndex, out centerBucketX, out centerBucketY);

		bucketIndices[0] = centerBucketIndex + PARTICLE_BUCKET_COUNT_X - 1;
		bucketIndices[1] = centerBucketIndex + PARTICLE_BUCKET_COUNT_X;
		bucketIndices[2] = centerBucketIndex + PARTICLE_BUCKET_COUNT_X + 1;
		bucketIndices[3] = centerBucketIndex - 1;
		bucketIndices[4] = centerBucketIndex;
		bucketIndices[5] = centerBucketIndex + 1;
		bucketIndices[6] = centerBucketIndex - PARTICLE_BUCKET_COUNT_X - 1;
		bucketIndices[7] = centerBucketIndex - PARTICLE_BUCKET_COUNT_X;
		bucketIndices[8] = centerBucketIndex - PARTICLE_BUCKET_COUNT_X + 1;

		bucketGridPosXs[0] = centerBucketX - 1;
		bucketGridPosYs[0] = centerBucketY + 1;

		bucketGridPosXs[1] = centerBucketX;
		bucketGridPosYs[1] = centerBucketY + 1;
		
		bucketGridPosXs[2] = centerBucketX + 1;
		bucketGridPosYs[2] = centerBucketY + 1;
		
		bucketGridPosXs[3] = centerBucketX - 1;
		bucketGridPosYs[3] = centerBucketY;
		
		bucketGridPosXs[4] = centerBucketX;
		bucketGridPosYs[4] = centerBucketY;
		
		bucketGridPosXs[5] = centerBucketX + 1;
		bucketGridPosYs[5] = centerBucketY;
		
		bucketGridPosXs[6] = centerBucketX - 1;
		bucketGridPosYs[6] = centerBucketY - 1;
		
		bucketGridPosXs[7] = centerBucketX;
		bucketGridPosYs[7] = centerBucketY - 1;
		
		bucketGridPosXs[8] = centerBucketX + 1;
		bucketGridPosYs[8] = centerBucketY - 1;

		int addedCount = 0;
		for (int clusterIndex = 0; clusterIndex < BUCKET_CLUSTER_SIZE; clusterIndex++){
			int bucketIndex = bucketIndices[clusterIndex];
			int bucketGridPosX = bucketGridPosXs[clusterIndex];
			int bucketGridPosY = bucketGridPosYs[clusterIndex];
			bool isWithinGrid = bucketGridPosX >= 0 && bucketGridPosY >= 0 && bucketGridPosX < PARTICLE_BUCKET_COUNT_X && bucketGridPosY < PARTICLE_BUCKET_COUNT_X;

			if(!isWithinGrid) continue;

			int[] bucketContent = particleBuckets[bucketIndex].GetContent();
			for (int contentIndex = 0; contentIndex < bucketContent.Length; contentIndex++){
				int content = bucketContent[contentIndex];
				if(content == -1) break;
				bucketClusterContent[addedCount] = content;
				addedCount++;
			}
		}
		if (addedCount < bucketClusterContent.Length){
			for (int i = addedCount; i < bucketClusterContent.Length; i++){
				bucketClusterContent[i] = -1;
			}
		}

		return bucketClusterContent;
	}

	void GetParticleBucketIndex(Vector2 pos, out int index, out int x, out int y) {
		float bucketWidth = (float)PARTICLE_BUCKET_WIDTH;
		x = Mathf.FloorToInt(pos.x / bucketWidth);
		y = Mathf.FloorToInt(pos.y / bucketWidth);
		index = y * PARTICLE_BUCKET_COUNT_X + x;
	}
}