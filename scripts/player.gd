extends CharacterBody3D


const MAX_SPEED: float = 50.
const MIN_SPEED: float = 0.
const ROLL_SPEED: float = 2.
const PITCH_SPEED: float = 2.
const YAW_SPEED: float = 1.
const ACCEL: float = 15.

@onready var jet_model = $Jet
@onready var initial_camera_pos = $ThirdPersonCam.position
@onready var third_person_cam = $ThirdPersonCam
@onready var explosion_ps = preload("res://scenes/explosion.tscn")

var roll_left: bool = false
var roll_right: bool = false
var pitch_up: bool = false
var pitch_down: bool = false
var yaw_left: bool = false
var yaw_right: bool = false
var accelerate: bool = false
var decelerate: bool = false
var speed: float = MIN_SPEED
var is_firing: bool = false

func _process(_delta):
	if Input.is_action_pressed("roll_left"): roll_left = true
	if Input.is_action_pressed("roll_right"): roll_right = true
	if Input.is_action_pressed("pitch_up"): pitch_up = true
	if Input.is_action_pressed("pitch_down"): pitch_down = true
	if Input.is_action_pressed("yaw_left"): yaw_left = true
	if Input.is_action_pressed("yaw_right"): yaw_right = true
	if Input.is_action_pressed("accelerate"): accelerate = true
	if Input.is_action_pressed("decelerate"): decelerate = true
	
	if Input.is_action_just_pressed("change_camera"):
		if get_viewport().get_camera_3d() == $ThirdPersonCam:
			$Jet/NoseCam.make_current()
		else:
			$ThirdPersonCam.make_current()
	
	if Input.is_action_pressed("fire"):
		is_firing = true
		$Jet/Gun.fire()
	else:
		is_firing = false

func _physics_process(delta):
	if roll_left:
		roll_left = false
		rotate(transform.basis.z, ROLL_SPEED * delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, 0, .6), delta)
	if roll_right:
		roll_right = false
		rotate(transform.basis.z, -ROLL_SPEED * delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, 0, -.6), delta)
	if pitch_up:
		pitch_up = false
		rotate(transform.basis.x, PITCH_SPEED * delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(.4, 0, 0), delta)
	if pitch_down:
		pitch_down = false
		rotate(transform.basis.x, -PITCH_SPEED * delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(-.4, 0, 0), delta)
	if yaw_left:
		yaw_left = false
		rotate(transform.basis.y, YAW_SPEED * delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, .4, .8), delta)
	if yaw_right:
		yaw_right = false
		rotate(transform.basis.y, -YAW_SPEED * delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, -.4, -.8), delta)
	
	if accelerate:
		accelerate = false
		speed = min(speed + ACCEL * delta, MAX_SPEED)
	if decelerate:
		decelerate = false
		speed = max(speed - ACCEL * delta, MIN_SPEED)
	
	velocity = -transform.basis.z * speed
	jet_model.rotation = jet_model.rotation.lerp(Vector3.ZERO, delta * 4)
	
	move_and_slide()
