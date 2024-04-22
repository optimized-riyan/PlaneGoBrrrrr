extends Node3D


var is_firing = false

func _process(_delta):
	if is_firing:
		$Jet/Gun.fire()
