extends CharacterBody3D


const SPEED = 10.
const ROLL_SPEED = 2.
const TURN_SPEED = 2.


var roll_left: bool = false
var roll_right: bool = false
var nose_up: bool = false
var nose_down: bool = false

func _process(delta):
	if Input.is_action_pressed("roll_left"): roll_left = true
	if Input.is_action_pressed("roll_right"): roll_right = true
	if Input.is_action_pressed("nose_up"): nose_up = true
	if Input.is_action_pressed("nose_down"): nose_down = true

func _physics_process(delta):
	if roll_left:
		roll_left = false
		rotate(transform.basis.z, ROLL_SPEED * delta)
	if roll_right:
		roll_right = false
		rotate(transform.basis.z, -ROLL_SPEED * delta)
	if nose_up:
		nose_up = false
		rotate(transform.basis.x, TURN_SPEED * delta)
	if nose_down:
		nose_down = false
		rotate(transform.basis.x, -TURN_SPEED * delta)
	
	velocity = -transform.basis.z * SPEED
	move_and_slide()
