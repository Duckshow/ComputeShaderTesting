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

		public int bucketIndex;

		public ElementParticle(float x, float y) {
			pos = new Vector2(x, y);
			velocity = new Vector2();
			force = new Vector2();
			density = 0;
			pressure = 0;
			bucketIndex = -1;
		}

		// public static int GetStride() {
		// 	return sizeof(float) * 14;
		// }
	};

	// solver parameters
	private static readonly Vector2 G = new Vector2(0, 300 * -9.8f); // external (gravitational) forces
	private const float REST_DENS = 1000.0f; // rest density
	private const float GAS_CONST = 2000.0f; // const for equation of state
	private const float H = 0.5f; // interaction radius
	private const float HSQ = H * H; // radius^2 for optimization
	private const float MASS = 2.0f; // assume all particles have the same mass
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
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 1;
	private const int GRID_HEIGHT_TILES = 1;
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


	void Start () {
		InitSPH();
		InitShader();
	}

	void InitShader() { 
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);
	}

	void InitSPH(){
		for (int i = 0; i < particleBuckets.Length; i++){
			particleBuckets[i] = new Bucket();
		}

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
		// for (int i = 0; i < GRID_HEIGHT_PIXELS; i++){
		// 	if(startParticles.Count >= MAX_PARTICLES) break;
		// 	for (int i2 = 0; i2 < GRID_WIDTH_PIXELS; i2++){
		// 		if(startParticles.Count >= MAX_PARTICLES) break;

		// 		float jitter = Random.value / H;
		// 		float newX = Mathf.Clamp(i2 + jitter, 0, GRID_WIDTH_PIXELS);
		// 		startParticles.Add(new ElementParticle(newX, i));
		// 	}
		// }

		particles = startParticles.ToArray();
	}

	void FixedUpdate () {
		CacheParticlesInBuckets();
		ComputeDensityAndPressure();
		ComputeForces();
		Integrate();
		// for (int i = 0; i < particles.Length; i++){
		// 	int[] neighbors = GetNeighborParticleIndices(particles[i].pos);
		// }

        particleSystem.Clear();
        ParticleSystem.MainModule main = particleSystem.main;
        main.startSize = H * 2;
        particleSystem.Emit(particles.Length);

        ParticleSystem.Particle[] emittedParticles = new ParticleSystem.Particle[particleSystem.particleCount];
        particleSystem.GetParticles(emittedParticles);
        for (int i = 0; i < particleSystem.particleCount; i++){
            emittedParticles[i].position = particles[i].pos;
//			emittedParticles[i].startColor = new Color((Mathf.Clamp((float)particles[i].bucketIndex / 255.0f, 0, 1)), 0, 0, 1);
			emittedParticles[i].startColor = new Color((Mathf.Clamp((float)particleBuckets[particles[i].bucketIndex].GetContentAmount() * 2 / 255.0f, 0, 1)), 0, 0, 1);
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
				ElementParticle otherParticle = particles[neighborIndices[i2]];
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
			int[] neighborIndices = GetNeighborParticleIndices(particle.pos);
			for (int i2 = 0; i2 < neighborIndices.Length; i2++){ // optimization: replace with grid and iterating over particles in neighboring grid-tiles
				int neighborIndex = neighborIndices[i2];
				ElementParticle otherParticle = particles[neighborIndex];
				if(i == neighborIndex) continue;

				Vector2 diff = otherParticle.pos - particle.pos;
				float r = diff.magnitude;

				if (r < H){
					Vector2 antiDir = -diff.normalized;
					// if (antiDir.x == 0 && antiDir.y == 0){
					//  	antiDir.x = -Random.value;
					// 	antiDir.y = -Random.value;
					// }
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
		private const int MAX_AMOUNT_OF_CONTENT = 32;
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
	int[] GetNeighborParticleIndices(Vector2 pos) {
		int centerBucketIndex;
		int centerBucketX;
		int centerBucketY;
		GetParticleBucketIndex(pos, out centerBucketIndex, out centerBucketX, out centerBucketY);

		int[] bucketGridPosXs = new int[BUCKET_CLUSTER_SIZE];
		int[] bucketGridPosYs = new int[BUCKET_CLUSTER_SIZE]; 
		int[] bucketIndices = new int[BUCKET_CLUSTER_SIZE];
		int[][] bucketCluster = new int[BUCKET_CLUSTER_SIZE][];
		bool[] shouldIncludes = new bool[BUCKET_CLUSTER_SIZE];

		int amountOfBucketsToMoveVertically = PARTICLE_BUCKET_COUNT_X; // minus to account for the center bucket

		bucketIndices[0] = centerBucketIndex + amountOfBucketsToMoveVertically - 1;
		bucketIndices[1] = centerBucketIndex + amountOfBucketsToMoveVertically;
		bucketIndices[2] = centerBucketIndex + amountOfBucketsToMoveVertically + 1;
		bucketIndices[3] = centerBucketIndex - 1;
		bucketIndices[4] = centerBucketIndex;
		bucketIndices[5] = centerBucketIndex + 1;
		bucketIndices[6] = centerBucketIndex - amountOfBucketsToMoveVertically - 1;
		bucketIndices[7] = centerBucketIndex - amountOfBucketsToMoveVertically;
		bucketIndices[8] = centerBucketIndex - amountOfBucketsToMoveVertically + 1;

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

		int totalContentCount = 0;
		for (int i = 0; i < BUCKET_CLUSTER_SIZE; i++){
			int bucketIndex = bucketIndices[i];
			int bucketGridPosX = bucketGridPosXs[i];
			int bucketGridPosY = bucketGridPosYs[i];
			bool isWithinGrid = bucketGridPosX >= 0 && bucketGridPosY >= 0 && bucketGridPosX < PARTICLE_BUCKET_COUNT_X && bucketGridPosY < PARTICLE_BUCKET_COUNT_X;
			shouldIncludes[i] = isWithinGrid;

			if(isWithinGrid) {
				bucketCluster[i] = particleBuckets[bucketIndex].GetContent();
				totalContentCount += particleBuckets[bucketIndex].GetContentAmount();
			}
			else { 
				bucketCluster[i] = null;
			}
		}

		int addedCount = 0;
		int[] bucketClusterContent = new int[totalContentCount];
		for (int i = 0; i < BUCKET_CLUSTER_SIZE; i++){
			if(!shouldIncludes[i]) continue;

			int[] bucketContent = bucketCluster[i];
			for (int i2 = 0; i2 < bucketContent.Length; i2++){
				int content = bucketContent[i2];
				if(content == -1) break;
				bucketClusterContent[addedCount] = content;
				addedCount++;
			}
		}

		// if (pos.x == 31 && pos.y == 31){
		// 	for (int i = 0; i < bucketIndices.Length; i++){
		// 		Debug.Log(bucketIndices[i] + ": " + shouldIncludes[i]);
		// 		if (shouldIncludes[i]){
		// 			particleBuckets[bucketIndices[i]].debug = true;
		// 		}
		// 	}
		// }

//		Debug.Log(pos + ": " + totalContentCount);
		return bucketClusterContent;
	}

	void GetParticleBucketIndex(Vector2 pos, out int index, out int x, out int y) {
		float bucketWidth = (float)PARTICLE_BUCKET_WIDTH;
		x = Mathf.FloorToInt(pos.x / bucketWidth);
		y = Mathf.FloorToInt(pos.y / bucketWidth);
		index = y * PARTICLE_BUCKET_COUNT_X + x;
	}
}