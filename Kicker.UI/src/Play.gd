extends Button

func _on_Play_pressed() -> void:
	var parent = get_parent();
	
	for child in parent.get_children():
		if child is ViewportContainer:
			for c in child.get_children():
				if c is ColorRect:
					child.remove_child(c)
	
	parent.remove_child(self);
