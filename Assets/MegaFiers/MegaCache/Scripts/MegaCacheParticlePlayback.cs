
using UnityEngine;
using System.IO;
using System.Collections.Generic;

[AddComponentMenu("MegaCache/Particle Playback")]
public class MegaCacheParticlePlayback : MegaCacheParticle
{
	public ParticleSystem particle;
	ParticleSystem.Particle[] particles;


	[ContextMenu("Help")]
	public void Help()
	{
		Application.OpenURL("http://www.west-racing.com/mf/?page_id=6222");
	}

	void Start()
	{
		if ( particle == null )
			particle = GetComponent<ParticleSystem>();

		particles = new ParticleSystem.Particle[image.maxparticles];

		if ( image )
			particle.Emit(image.maxparticles);
	}

	void LateUpdate()
	{
		if ( particle && image )
		{
			if ( image.optimized )
				UpdateParticlesOpt(Time.deltaTime);
			else
				UpdateParticles(Time.deltaTime);
		}
	}

	void UpdateParticles(float dt)
	{
		if ( dt > 0.01f )
			dt = 0.01f;

		if ( particle && image && image.particles.Count > 0 )
		{
			particle.GetParticles(particles);

			int ix = 0;

			Matrix4x4 tm = transform.localToWorldMatrix;

			time += Time.deltaTime * speed;

			float len = image.frames / fps;

			float t = 0.0f;

			switch ( loopmode )
			{
				case MegaCacheRepeatMode.Loop:		t = Mathf.Repeat(time, len); break;
				case MegaCacheRepeatMode.Clamp:		t = Mathf.Clamp(time, 0.0f, len); break;
				case MegaCacheRepeatMode.PingPong:	t = Mathf.PingPong(time, len); break;
			}

			float alpha = t / len;

			float fn = alpha * (image.frames - 1);
			framenum = (int)fn;

			Debug.Log("fn " + framenum);
			MegaCacheParticleHistory ph = image.particles[framenum];
			Debug.Log("fn " + framenum + " count " + ph.positions.Count);



			for ( int i = 0; i < ph.positions.Count; i++ )
			{
				particles[ix].position = tm.MultiplyPoint3x4(ph.positions[i] * scaleall);
#if UNITY_2017 || UNITY_2018
				particles[ix].remainingLifetime = alpha * len;
#else
				particles[ix].lifetime = alpha * len;
#endif
				particles[ix].startLifetime = len;
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017 || UNITY_2018
				particles[ix].startSize = ph.scale[i] * scaleall * sizescale;
#else
				particles[ix].size = ph.scale[i] * scaleall * sizescale;
#endif
				//particles[ix].rotation = ph.rots[i][(int)axis] * Mathf.Rad2Deg;

#if UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017 || UNITY_2018
				particles[ix].rotation3D = ph.rots[i];
#else
				particles[ix].rotation = ph.rots[i][(int)axis];	// * Mathf.Rad2Deg;
#endif

				ix++;
			}

			particle.SetParticles(particles, ix);
		}
	}

	// Should be in another class
	void UpdateParticlesOpt(float dt)
	{
		if ( dt > 0.01f )
			dt = 0.01f;

		if ( particle && image && image.optparticles.Count > 0 )
		{
			particle.GetParticles(particles);

			time += Time.deltaTime * speed;

			float len = image.frames / fps;

			float t = 0.0f;

			switch ( loopmode )
			{
				case MegaCacheRepeatMode.Loop: t = Mathf.Repeat(time, len); break;
				case MegaCacheRepeatMode.Clamp: t = Mathf.Clamp(time, 0.0f, len); break;
				case MegaCacheRepeatMode.PingPong: t = Mathf.PingPong(time, len); break;
			}

			float alpha = t / len;

			float fn = alpha * image.frames;
			framenum = (int)fn;

			int ix = 0;

			Matrix4x4 tm = transform.localToWorldMatrix;

			MegaCacheParticleHistoryOpt ph = image.optparticles[framenum];

			for ( int i = 0; i < ph.count; i++ )
			{
				Vector3 pos = DecodeV3(ph.pos, i * 6, ph.posmin, ph.possize);
				float scl = DecodeFloat(ph.scale, i, ph.scalemin, ph.scalesize);
				Vector3 rot = DecodeV3b(ph.rots, i * 3, ph.rotmin, ph.rotsize);

				particles[ix].position = tm.MultiplyPoint3x4(pos);
#if UNITY_2017 || UNITY_2018
				particles[ix].remainingLifetime = alpha * len;
#else
				particles[ix].lifetime = alpha * len;
#endif
				particles[ix].startLifetime = len;
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017 || UNITY_2018

				particles[ix].startSize = scl;
#else
				particles[ix].size = scl;
#endif
#if UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017 || UNITY_2018
				particles[ix].rotation3D = rot;
#else
				particles[ix].rotation = rot[(int)axis];	// * Mathf.Rad2Deg;
#endif

				ix++;
			}

			particle.SetParticles(particles, ix);
		}
	}

	void OnDrawGizmosSelected()
	{
		if ( showpaths && image )
		{
			Gizmos.color = Color.red;
			Gizmos.matrix = transform.localToWorldMatrix;
			Color col = showcolor;

			float len = image.frames / fps;

			float t = 0.0f;

			if ( time < 0.0f )
				time = 0.0f;

			switch ( loopmode )
			{
				case MegaCacheRepeatMode.Loop: t = Mathf.Repeat(time, len); break;
				case MegaCacheRepeatMode.Clamp: t = Mathf.Clamp(time, 0.0f, len); break;
				case MegaCacheRepeatMode.PingPong: t = Mathf.PingPong(time, len); break;
			}

			float alpha = t / len;

			//Debug.Log("frames " + image.frames);
			float fn = alpha * (image.frames - 1);
			framenum = (int)fn;

			if ( image.optimized )
			{
				MegaCacheParticleHistoryOpt ph = image.optparticles[framenum];

				for ( int j = 0; j < ph.count; j += showparticlestep )
				{
					Vector3 lpos = DecodeV3(ph.pos, j * 6, ph.posmin, ph.possize) * scaleall * emitscale;
					float scl = DecodeFloat(ph.scale, j, ph.scalemin, ph.scalesize);

					Gizmos.color = col;
					Gizmos.DrawSphere(lpos, scl * scaleall * sizescale);
				}
			}
			else
			{
				Debug.Log("fn " + framenum + " max " + image.particles.Count);
				MegaCacheParticleHistory ph = image.particles[framenum];

				for ( int j = 0; j < ph.positions.Count; j += showparticlestep )
				{
					Vector3	lpos = ph.positions[j] * scaleall * emitscale;

					Gizmos.color = col;
					Gizmos.DrawSphere(lpos, ph.scale[j] * scaleall * sizescale);
				}
			}
			Gizmos.matrix = Matrix4x4.identity;
		}
	}
}