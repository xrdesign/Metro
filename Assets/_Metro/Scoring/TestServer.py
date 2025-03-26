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

# start_time = time.time()
# count = 0
# while True:
#     result = get_state_from_game(ws, 0)
#     if result is None:
#         break
#     count += 1
#     # print the rate of requests per second
#     if count % 100 == 0:
#         print(count / (time.time() - start_time))
#     time.sleep(0.001)

test_response = ""
while True:
    test_response = input("Input text:")
    print(test_response)
    send_response(ws, test_response)



