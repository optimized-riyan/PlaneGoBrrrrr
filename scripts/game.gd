extends Node3D


@export var enemy_spawn_pos: Vector3

const TICK_RATE: float = 20.

var enemy_ps: PackedScene = preload("res://scenes/enemy.tscn")
var enemy_peer_id: int = 0
var enemy: Node3D
@onready var player: CharacterBody3D = $Player
var tick: float = 0

func _ready():
	multiplayer.multiplayer_peer = Global.peer
	multiplayer.peer_connected.connect(_on_player_connected)
	multiplayer.peer_disconnected.connect(_on_player_disconnected)
	if !multiplayer.is_server():
		multiplayer.server_disconnected.connect(_on_server_disconnected)

func _process(delta):
	if enemy_peer_id != 0:
		tick += TICK_RATE / delta
		if tick >= 1000.:
			sync()
			tick -= 1000.

func _on_player_connected(player_id: int):
	enemy_peer_id = player_id
	enemy = enemy_ps.instantiate()
	enemy.position = enemy_spawn_pos
	add_child(enemy)

func _on_player_disconnected(_player_id: int):
	enemy.queue_free()
	enemy_peer_id = 0

func _on_server_disconnected():
	get_tree().quit()

@rpc("any_peer", "unreliable_ordered", "call_remote", 0)
func update_enemy(values: Dictionary):
	var tween: Tween = get_tree().create_tween()
	tween.tween_property(enemy, "position", values["position"], .1)
	tween.tween_property(enemy, "rotation", values["rotation"], .1)
	tween.tween_property(enemy.get_node("Jet"), "rotation", values["model_rotation"], .1)
	enemy.is_firing = values["fire"]

func sync():
	var values: Dictionary = {
		"position": player.position,
		"rotation": player.rotation,
		"model_rotation": player.get_node("Jet").rotation,
		"fire": player.is_firing
	}
	update_enemy.rpc_id(enemy_peer_id, values)
