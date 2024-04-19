extends CharacterBody3D


const SPEED = 50.
const ROLL_SPEED = 2.
const PITCH_SPEED = 2.
const YAW_SPEED = 2.

@onready var jet_model = $Jet

var roll_left: bool = false
var roll_right: bool = false
var pitch_up: bool = false
var pitch_down: bool = false
var yaw_left: bool = false
var yaw_right: bool = false

func _process(delta):
	if Input.is_action_pressed("roll_left"): roll_left = true
	if Input.is_action_pressed("roll_right"): roll_right = true
	if Input.is_action_pressed("pitch_up"): pitch_up = true
	if Input.is_action_pressed("pitch_down"): pitch_down = true
	if Input.is_action_pressed("yaw_left"): yaw_left = true
	if Input.is_action_pressed("yaw_right"): yaw_right = true
	
	if Input.is_action_just_pressed("change_camera"):
		if get_viewport().get_camera_3d() == $NoseCam:
			$ThirdPersonCam.make_current()
		else:
			$NoseCam.make_current()

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
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, .4, 0), delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, 0, .8), delta)
	if yaw_right:
		yaw_right = false
		rotate(transform.basis.y, -YAW_SPEED * delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, -.4, 0), delta)
		jet_model.rotation = jet_model.rotation.lerp(Vector3(0, 0, -.8), delta)
	
	velocity = -transform.basis.z * SPEED
	velocity = velocity.move_toward(Vector3.ZERO, delta)
	
	jet_model.rotation = jet_model.rotation.lerp(Vector3.ZERO, delta * 4)
	move_and_slide()
