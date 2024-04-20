extends Node3D


@export var enemy_spawn_pos: Vector3

var enemy_ps: PackedScene = preload("res://scenes/enemy.tscn")
var enemy_peer_id: int
var enemy: CharacterBody3D
@onready var player: CharacterBody3D = $Player

func _ready():
	multiplayer.multiplayer_peer = Global.peer
	multiplayer.peer_connected.connect(_on_player_connected)
	multiplayer.peer_disconnected.connect(_on_player_disconnected)
	multiplayer.server_disconnected.connect(_on_server_disconnected)

func _on_player_connected(player_id: int):
	enemy_peer_id = player_id
	enemy = enemy_ps.instantiate()
	enemy.position = enemy_spawn_pos
	add_child(enemy)
	$NetworkTimer.start()

func _on_player_disconnected(_player_id: int):
	enemy.queue_free()
	$NetworkTimer.stop()

func _on_server_disconnected():
	get_tree().quit()

@rpc("any_peer", "unreliable", "call_remote", 0)
func update_enemy(values: Dictionary):
	enemy.position = values["position"]
	enemy.rotation = values["rotation"]
	enemy.get_node("Jet").rotation = values["model_rotation"]

func _on_network_timer_timeout():
	var values: Dictionary = {
		"position": player.position,
		"rotation": player.rotation,
		"model_rotation": player.get_node("Jet").rotation
	}
	update_enemy.rpc_id(enemy_peer_id, values)
