extends Sprite

func _ready() -> void:
	var parent := get_parent()
	var player_name : String = parent.name
	var s := player_name.split("-")
	
	if s[0] == "BVB" && int(s[1]) < 6:
		var path := "res://assets/player_sprites/Gelb-0%s.png"
		texture = load(path % s[1])
	elif s[0] == "ADESSO" && int(s[1]) < 6:
		var path := "res://assets/player_sprites/Blau-0%s.png"
		texture = load(path % s[1])
