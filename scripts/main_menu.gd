extends Control


@onready var game_ps: PackedScene = preload("res://scenes/game.tscn")

func _on_host_pressed():
	var peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	var error: Error = peer.create_server(Global.PORT, 2)
	if error:
		print(error)
		get_tree().quit()
	Global.peer = peer
	get_tree().change_scene_to_packed(game_ps)

func _on_join_pressed():
	var peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	var error: Error = peer.create_client(Global.IP_ADDR, Global.PORT)
	if error:
		print(error)
		get_tree().quit()
	Global.peer = peer
	get_tree().change_scene_to_packed(game_ps)

func _on_quit_pressed():
	get_tree().quit()
