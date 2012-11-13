using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class LightningArcs : MonoBehaviour {
	
	protected static float TWO_PI = Mathf.PI * 2f;
	
	private class Arc {
		
		private const float wiggleSpeed = 3f;
		private const float wiggleMag = 2f;
		private const int pointsPerArc = 5;
		private const float maxAngularVelocityPerSecond = 0.2f;
		protected const float outerShellWidth = 2f;
		protected const float innerShellWidth = 1f;
		
		private class ArcPoint {
			//requires tuning, roughly half the height of a capsule collider
			private const float farRadius = 5f;
			private const float farRadiusSquared = farRadius * farRadius;
			
			private Vector3 curPos;
			public Vector3 CurPos {
				get {
					return curPos;
				}
			}
			
			public Vector3 genPos; //format is row, phi, radius
			public Vector3 genVel;
			
			private Vector3 farPoint;
			
			public ArcPoint() {
				this.genPos = new Vector3();
				this.genVel = new Vector3();
				curPos = new Vector3();
				farPoint = new Vector3();
			}
			
			public void Update(Collider innerShell) {
				genPos += genVel * Time.deltaTime;
				farPoint.x = Mathf.Cos(genPos.y) * farRadius * Mathf.Cos(genPos.x);
				farPoint.y = Mathf.Sin(genPos.y) * farRadius * Mathf.Cos(genPos.x);
				farPoint.z = Mathf.Sin(genPos.x) * farRadius;
				curPos = innerShell.ClosestPointOnBounds(farPoint) * genPos.z;
				if (genPos.z > outerShellWidth) {
					genVel.z = -Mathf.Abs(genVel.z);
				}
				else if (genPos.z < innerShellWidth) {
					genVel.z = Mathf.Abs(genVel.z);
				}
			}
			
		}
		private ArcPoint[] points;
		private ParticleSystem.Particle[] particles;
		
		private int start;
		private int end;
		private float len;
		
		public Arc(ParticleSystem.Particle[] particles, int start, int count) {
			this.start = start;
			this.end = start + count;
			len = (float)count;
			this.particles = particles;
			points = new ArcPoint[pointsPerArc];
			for (int i = 0; i < pointsPerArc; i++) {
				points[i] = new ArcPoint();
			}
			initPoints();
		}
			
		private void initPoints() {
			for (int i = 0; i < pointsPerArc; i++) {
				points[i].genPos.x = Random.Range(0f, TWO_PI);
				points[i].genPos.y = Random.Range(0f, TWO_PI);
				points[i].genVel.x = Random.Range(0f, TWO_PI) * maxAngularVelocityPerSecond;
				points[i].genVel.y = Random.Range(0f, TWO_PI) * maxAngularVelocityPerSecond;
				if (i == 0 || i == pointsPerArc - 1) {
					points[i].genPos.z = 0f;
					points[i].genVel.z = 0f;
				}
				else {
					points[i].genPos.z = Random.Range(0f, outerShellWidth);
					points[i].genVel.z = Random.Range(0f, outerShellWidth) * maxAngularVelocityPerSecond;
				}
			}
		}
		
		public void Update(Collider innerShell) {
			foreach (ArcPoint p in points) {
				p.Update(innerShell);
			}
			for (int i = start; i < end; i++) {
				float totalDistance = ((float)((i - start) * (points.Length - 1))) / ((float)(end - start));
				int pointNum = Mathf.FloorToInt(totalDistance);
				float partialDistance = totalDistance - (float)pointNum;
				Vector3 position = Vector3.Lerp(points[pointNum].CurPos, points[pointNum + 1].CurPos, partialDistance);
				position.x +=  noise.Noise(Time.time * wiggleSpeed + position.x, position.y, position.z) * wiggleMag;
				position.y +=  noise.Noise(position.x, Time.time * wiggleSpeed + position.y, position.z) * wiggleMag;
				position.z +=  noise.Noise(position.x, position.y, Time.time * wiggleSpeed + position.z) * wiggleMag;
				particles[i].position = position;
			}
		}
	}
	
	public int particlesPerArc;
	public int maxConcurrentArcs;
	
	private int pCount {
		get {
			return particlesPerArc * maxConcurrentArcs;
		}
	}
	
	private Arc[] arcs;
	ParticleSystem.Particle[] particles;
	
	protected static Perlin noise;
	
	//TODO
	private IEnumerable<Arc> activeArcs {
		get {
			return arcs;
		}
	}
	
	void Start () {
		if (noise == null) {
			noise = new Perlin();
		}
		
		particles = new ParticleSystem.Particle[pCount];
		particleSystem.Emit(pCount);
		if (particleSystem.GetParticles(particles) < pCount) {
			Debug.Log("Hey!");
		}
		arcs = new Arc[maxConcurrentArcs];
		for (int i = 0; i < maxConcurrentArcs; i++) {
			arcs[i] = new Arc(particles, particlesPerArc * i, particlesPerArc);
		}
	}
	
	void Update () {
		foreach (Arc a in activeArcs) {
			a.Update(collider);
		}
		particleSystem.SetParticles(particles, pCount);
	}
}