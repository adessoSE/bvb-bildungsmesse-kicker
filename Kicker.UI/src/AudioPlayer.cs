using Godot;

public class AudioPlayer : AudioStreamPlayer
{
	private AudioStream[] audioFiles;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var formatLoader = new ResourceFormatLoader();
		var jubel = ResourceLoader.Load<AudioStream>("res://assets/audio/jubel_test.mp3", formatLoader.GetResourceType("res://assets/audio/jubel_test.mp3"));
		var kick = ResourceLoader.Load<AudioStream>("res://assets/audio/kick_test.mp3", formatLoader.GetResourceType("res://assets/audio/kick_test.mp3"));
		var running = ResourceLoader.Load<AudioStream>("res://assets/audio/running_test.mp3", formatLoader.GetResourceType("res://assets/audio/running_test.mp3"));
		
		audioFiles = new AudioStream[3] { jubel, kick, running};
	}

	public void PlayGoalAudio()
	{
		Stream = audioFiles[0];
		Play();
	}
}
