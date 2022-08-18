extends Node

export onready var audio_player = $AudioStreamPlayer

export onready var jubel = preload("res://assets/audio/jubel_test.mp3")
export onready var kick = preload("res://assets/audio/kick_test.mp3")
export onready var running = preload("res://assets/audio/running_test.mp3")

func _play_audio_track(s: String) -> void:
	if s == "Jubel" && !audio_player.playing:
		audio_player.stream = jubel
	
	elif s == "kick" && !audio_player.playing:
		audio_player.stream = kick
	
	elif s == "running" && !audio_player.playing:
		audio_player.stream = running
	
	audio_player.play()

func _stop_audio_stream() -> void:
	audio_player.stop()
