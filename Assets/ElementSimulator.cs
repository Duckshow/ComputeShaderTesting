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
	private static readonly Vector2 G = new Vector2(0, 600 * -9.8f); // external (gravitational) forces
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
	private const int MAX_PARTICLES = 250;
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
			particleBuckets[i] = new Bucket<int>();
		}

		List<ElementParticle> startParticles = new List<ElementParticle>();

		// for (float y = EPS; y < GRID_HEIGHT_PIXELS - EPS * 2.0f; y += H) {
		// 	for (float x = EPS; x < GRID_WIDTH_PIXELS * 0.5f - EPS * 2.0f; x += H) {
		// 		if (startParticles.Count < Mathf.Min(DAM_PARTICLES, MAX_PARTICLES)){
		// 			// if(Random.value < (x / GRID_WIDTH_PIXELS) * 4) continue;
		// 			float jitter = 0;// Random.value / H;
		// 			startParticles.Add(new ElementParticle(x + jitter, y));
		// 		}
		// 	}
		// }
		for (int i = 0; i < GRID_HEIGHT_PIXELS; i++){
			for (int i2 = 0; i2 < GRID_WIDTH_PIXELS; i2++){
				startParticles.Add(new ElementParticle(i, i2));
			}
		}

		particles = startParticles.ToArray();
	}

	void FixedUpdate () {
		CacheParticlesInBuckets();
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
			emittedParticles[i].startColor = new Color((Mathf.Clamp((float)particles[i].bucketIndex * 2 / 255.0f, 0, 1)), 0, 0, 1);
		}
        particleSystem.SetParticles(emittedParticles, particleSystem.particleCount);

		for (int i = 0; i < particleBuckets.Length; i++){
			Debug.Log("Bucket #" + i + ": " + particleBuckets[i].GetContent().Length);
		}

		Debug.LogError("");
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
				ElementParticle otherParticle = particles[neighborIndices[i2]];
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

	private const int GRID_TOTAL_SIZE = GRID_WIDTH_PIXELS * GRID_HEIGHT_PIXELS;
	private const int PARTICLE_BUCKET_SIZE = 4; // relative to H
	private const int PARTICLE_BUCKET_WIDTH = 2;
	private const int PARTICLE_BUCKET_COUNT = GRID_TOTAL_SIZE / PARTICLE_BUCKET_SIZE;
	private const int PARTICLE_BUCKET_COUNT_X = GRID_WIDTH_PIXELS / PARTICLE_BUCKET_WIDTH;
	private static Bucket<int>[] particleBuckets = new Bucket<int>[GRID_TOTAL_SIZE / PARTICLE_BUCKET_SIZE];
	class Bucket<T> {
		private List<T> contentList = new List<T>();
		private T[] contentArray;
		public void Clear() {
			contentList = new List<T>();
			contentArray = null;
		}
		public void AddContent(T newContent) {
			contentList.Add(newContent);
		}
		public void SetContentArray() {
			contentArray = contentList.ToArray();
		}
		public T[] GetContent() {
			return contentList.ToArray();
		}
	}

	void CacheParticlesInBuckets() {
		for (int i = 0; i < particleBuckets.Length; i++){
			particleBuckets[i].Clear();
		}
		for (int i = 0; i < particles.Length; i++){
			ElementParticle particle = particles[i];
			int index = GetParticleBucketIndex(particle.pos);
			particles[i].bucketIndex = index;
			particleBuckets[index].AddContent(i);
			//particleBuckets[0].AddContent(i);
		}
		for (int i = 0; i < particleBuckets.Length; i++){
			particleBuckets[i].SetContentArray();
		}
	}

	private const int BUCKET_CLUSTER_SIZE = 9;
	int[] GetNeighborParticleIndices(Vector2 pos) {
		//return particleBuckets[0].GetContent();
		int bucketIndex = GetParticleBucketIndex(pos);
		int[] bucketIndices = new int[BUCKET_CLUSTER_SIZE];
		int[][] bucketCluster = new int[BUCKET_CLUSTER_SIZE][];
		bool[] shouldIncludes = new bool[BUCKET_CLUSTER_SIZE];

		bucketIndices[0] = bucketIndex + PARTICLE_BUCKET_COUNT_X - 1;
		bucketIndices[1] = bucketIndex + PARTICLE_BUCKET_COUNT_X;
		bucketIndices[2] = bucketIndex + PARTICLE_BUCKET_COUNT_X + 1;
		bucketIndices[3] = bucketIndex - 1;
		bucketIndices[4] = bucketIndex;
		bucketIndices[5] = bucketIndex + 1;
		bucketIndices[6] = bucketIndex - PARTICLE_BUCKET_COUNT_X - 1;
		bucketIndices[7] = bucketIndex - PARTICLE_BUCKET_COUNT_X;
		bucketIndices[8] = bucketIndex - PARTICLE_BUCKET_COUNT_X + 1;

		int totalContentCount = 0;
		for (int i = 0; i < BUCKET_CLUSTER_SIZE; i++){
			bool shouldInclude = IsIndexWithinArray(bucketIndices[i], particleBuckets.Length);
			shouldIncludes[i] = shouldInclude;

			if(shouldInclude) {
				bucketCluster[i] = particleBuckets[bucketIndices[i]].GetContent();
				totalContentCount += bucketCluster[i].Length;
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
				bucketClusterContent[addedCount] = bucketContent[i2];
				addedCount++;
			}
		}

		return bucketClusterContent;
	}

	int GetParticleBucketIndex(Vector2 pos) {
		int x = (int)(pos.x / (float)(PARTICLE_BUCKET_WIDTH * 0.5f));
		int y = (int)(pos.y / (float)(PARTICLE_BUCKET_WIDTH * 0.5f));
//		Debug.Log(pos + ": " + x + ", " + y);
		return y * PARTICLE_BUCKET_WIDTH + x;
	}

	bool IsIndexWithinArray(int index, int arrayLength) {
		return index >= 0 && index < arrayLength;
	}
}