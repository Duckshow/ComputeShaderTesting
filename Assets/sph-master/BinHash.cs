using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BinHash : MonoBehaviour {

  /*@T
  * \section{Spatial hashing}
  *
  * We conceptually partition our computational domain into bins that
  * are at least $h$ on a side, and label each with integer coordinates
  * $(i_x, i_y, i_z)$.  In three dimensions, the bin size doesn't have
  * to be all that small before the number of such bins is quite large,
  * and most bins will be empty.  Rather than represent each bin
  * explicitly, we will map the bins to locations into a hash table,
  * allowing the possibility that different bins can map to the same
  * location in the hash table (though ideally this should not happen
  * too often).  We compute the hash function by mapping $(i_x, i_y, i_z)$ 
  * to a Z-Morton integer index, which we then associate with a hash
  * bucket.  By figuring out the hash buckets in which the neighbor of
  * a given particle could possibly lie, we significantly reduce the
  * cost of checking interactions.
  *
  * In the current implementation, we set the bin size equal to $h$,
  * which implies that particles interacting with a given particle
  * might lie in any of 27 possible neighbors.  If you use a bin of
  * size $2h$, you may have more particles in each bin, but you would
  * only need to check eight bins (at most) for possible interactions.
  * In order to allow for the possibility of more or fewer possible
  * bins containing neighbors, we define [[MAX_NBR_BINS]] to be the
  * maximum number of bins we will ever need to check for interactions,
  * and let [[particle_neighborhood]] return both which bins are
  * involved (as an output argument) and the number of bins needed (via
  * the return value).
  *
  * We also currently use the last few bits of each of the $i_x, i_y,
  * i_z$ indices to form the Z-Morton index.  You may want to change
  * the number of bits used, or change to some other hashing scheme
  * that potentially spreads the bins more uniformly across the table.
  *
  *@c*/


  /*@T
  * \subsection{Spatial hashing implementation}
  * 
  * In the current implementation, we assume [[HASH_DIM]] is $2^b$,
  * so that computing a bitwise of an integer with [[HASH_DIM]] extracts
  * the $b$ lowest-order bits.  We could make [[HASH_DIM]] be something
  * other than a power of two, but we would then need to compute an integer
  * modulus or something of that sort.
  * 
  *@c*/

  public const uint HASH_DIM = 0x10; // 16
  public const uint HASH_SIZE = (HASH_DIM*HASH_DIM*HASH_DIM);
  public const uint MAX_NBR_BINS = 27;
  public const uint HASH_MASK = (HASH_DIM-1);

  static uint particle_bucket(State.particle_t p, float h){
      uint ix = (uint)(p.x.x / h);
      uint iy = (uint)(p.x.y / h);
      uint iz = (uint)(p.x.z / h);
      return ZMorton.zm_encode(ix & HASH_MASK, iy & HASH_MASK, iz & HASH_MASK);
  }

  // Note: We check ALL buckets, even those that are weird...
  public static void particle_neighborhood(ref uint[] buckets, State.particle_t p, float h){
	//printf("called!");
	uint ix = (uint)(p.x.x / h);
    uint iy = (uint)(p.x.y / h);
    uint iz = (uint)(p.x.z / h);

    int counter = 0;
    for (int i = -1; i < 2; i++) {
      for (int j = -1; j < 2; j++) {
        for (int k = -1; k < 2; k++) {
          uint x = (uint)(ix + i);
          uint y = (uint)(iy + j);
          uint z = (uint)(iz + k);

          buckets[counter] = ZMorton.zm_encode(x & HASH_MASK,y & HASH_MASK,z & HASH_MASK);
          counter += 1;
        }
      }
    }
  }

  public static void hash_particles(State.sim_state_t s, float h){
    // Unpack particles and hash
    //particle_t* p = s.part;
    //particle_t** hash = s.hash;
    int n = s.n;

    // First clear hashtable (TODO: Make this faster)
    for (int i = 0; i < HASH_SIZE; i++)
      s.hash[i] = null;

    // Loop through particles to hash
    for (int i = 0; i < n; i++) {
      // Some error output on the y-axis
      // Had some errors when working for CS5643 going into into slightly negative values
     // if (s.part[i].x[1] < 0) {
        //if (p[i].x[1] < 1e-5) {
          //printf("ERROR HASH WILL FAIL: Particle: %e %e %e\n", \
          // s.part[i].x[0], s.part[i].x[1], s.part[i].x[2]);
        //}
        //p[i].x[1] = 0;
      //}

      // Hash using Z Morton
      uint b = particle_bucket(s.part[i], h);

      // Add particle to the start of the list of bin b
      s.part[i].next = s.hash[b];
      s.hash[b] = s.part[i];
    }
  }

}
