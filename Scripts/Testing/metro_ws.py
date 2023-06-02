import websocket
import time

def send_and_recv(ws, message):
    ws.send(message)
    print("Sent " + message)
    print(ws.recv())

# async main
def main():
    # connect to the metro ws server at ws://localhost:3000/metro
    ws = websocket.create_connection('ws://localhost:3000/metro')

    # print when connection is ready
    print('connected')
    # test get_state and print the result
    # send_and_recv(ws, '{"command":"get_state", "game_id":0}')

    # test get_actions and print the result
    send_and_recv(ws, '{"command":"get_potential_actions"}')

    # test take_action connecting station 0 to station 1 with line 0
    send_and_recv(ws, '{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":0, "insert_index":0}}')
    send_and_recv(ws, '{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":1, "insert_index":1}}')
    send_and_recv(ws, '{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":2, "insert_index":2}}')
    
    # Get Action queue
    send_and_recv(ws, '{"command":"get_action_finished", "action_id":2}')
    send_and_recv(ws, '{"command":"get_action_queue"}')
    time.sleep(1) # Sleep for 3 seconds
    send_and_recv(ws, '{"command":"get_action_finished", "action_id":2}')
    send_and_recv(ws, '{"command":"get_action_queue"}')
    
    
    # test take_action connecting station 2 to station 0 with line 1
    # send_and_recv(ws, '{"command":"take_action", "arguments":{"action":"insert_station", "line":1, "station":2, "index":0}}')
    # send_and_recv(ws, '{"command":"take_action", "arguments":{"action":"insert_station", "line":1, "station":0, "index":1}}')
    
    # test get_state and print the result (take_action should have changed the state)
    # send_and_recv(ws, '{"command":"get_state"}')
    
main()
