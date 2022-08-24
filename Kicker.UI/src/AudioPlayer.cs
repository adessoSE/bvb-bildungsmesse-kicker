using System;
using System.Linq;
using Godot;
using Kicker.UI;

public class AudioPlayer : AudioStreamPlayer
{
	private AudioStream _jubel;
	private AudioStream _kick;
	private AudioStream[] _running;
	private readonly ResourceFormatLoader _formatLoader;
	
	private const int Soft = -20;

	public AudioPlayer()
	{
		_formatLoader = new ResourceFormatLoader();
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_jubel = LoadAudio("jubelkick.wav");
		_kick = LoadAudio("kick.wav");
		_running = Enumerable.Range(1, 15).Select(i => LoadAudio($"run{i}.wav")).ToArray();
	}

	private AudioStream LoadAudio(string name)
	{
		var path = $"res://assets/audio/{name}";
		var format = _formatLoader.GetResourceType(path);
		return ResourceLoader.Load<AudioStream>(path, format);
	}

	private void Play(AudioStream stream, float volume = 0, bool filter = false)
	{
		Stream = stream;
		VolumeDb = volume;
		Bus = filter ? "Filter" : "Master";
		Play();
	}

	public void PlayJubel() => Play(_jubel);
	public void PlayKickHard() => Play(_kick);
	public void PlayRunning(bool b) => Play(_running.Random(), Soft, filter: b);
}

