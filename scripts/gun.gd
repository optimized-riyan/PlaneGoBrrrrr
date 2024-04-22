extends RayCast3D


const FIRE_RATE: int = 10
const EXPLOSION_COUNT = 15

var firing_cooldown: int = FIRE_RATE
var explosion_ps: PackedScene = preload("res://scenes/explosion.tscn")
var explosion_array: Array = []
var explosion_array_ptr: int = 0

func _ready():
	for i in EXPLOSION_COUNT:
		explosion_array.append(explosion_ps.instantiate())
		add_child(explosion_array[-1])

func _process(_delta):
	firing_cooldown = firing_cooldown - 1 if firing_cooldown > 0 else 0

func fire():
	if firing_cooldown == 0:
		firing_cooldown = FIRE_RATE
		force_raycast_update()
		if is_colliding():
			explosion_array[explosion_array_ptr].global_position = get_collision_point()
			explosion_array[explosion_array_ptr].emitting = true
			explosion_array_ptr = (explosion_array_ptr + 1) % EXPLOSION_COUNT
