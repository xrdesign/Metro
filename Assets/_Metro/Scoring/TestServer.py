import websocket
import json
import time

url = 'ws://localhost:3000/metro'

def send_and_recv(ws, message: dict):
    message = json.dumps(message)
    attempts = 0
    while attempts < 3:
        try:
            ws.send(message)
            result = ws.recv()
            return json.loads(result)
        except:
            print(attempts)
            attempts = attempts + 1
            ws = websocket.create_connection(url)

    print("Failed")
    return None

def get_state_from_game(ws, game_id: int):
    state = send_and_recv(ws, {"command":"get_state_sync", "game_id": game_id})
    return state

def send_response(ws, response):
    message = {
        "command": "speak",
        "response" : response
    }
    ws.send(json.dumps(message))


ws = websocket.create_connection(url)


# get state from game every 1 second
while True:
    result = get_state_from_game(ws, 0)
    if result is None:
        break
    print(result)
    # check if there is a new instruction and if so, get the instruction text
    # var stateRes = new JSONObject();
    # try
    # {
    #     stateRes = MetroManager.SerializeGame(gameIDGetStateSync);
    #     stateRes.AddField("new_instructions", MetroManager.HasInstructions());
    #     stateRes.AddField("instruction_text", MetroManager.GetInstructions());

    #     Send(stateRes.ToString()); // Respond after Update() runs
    # }

    if result["new_instructions"]:
        instruction_text = result["instruction_text"]
        print(instruction_text)

        if "hello" in instruction_text.lower() or "hi" in instruction_text.lower() or "good morning" in instruction_text.lower() or "good afternoon" in instruction_text.lower():
            send_response(ws, "Hello, how can I help you?")
        elif "who" in instruction_text.lower():
            send_response(ws, "I am a virtual assistant")
        else:
            send_response(ws, "I am not sure how to help you")
    time.sleep(1)
