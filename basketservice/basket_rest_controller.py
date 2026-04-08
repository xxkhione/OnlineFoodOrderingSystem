from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv
from uuid import UUID, uuid4
import os
import redis
import json
import requests
from menu_item import MenuItem

load_dotenv()

REDIS_HOST = os.getenv("REDIS_HOST", "localhost")
REDIS_PORT = int(os.getenv("REDIS_PORT", 6379))
MENU_ITEM_SERVICE_URL = os.getenv("MENU_ITEM_SERVICE_URL", "http://localhost:5000/api/menu-items")


redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, db=0, decode_responses=True)


def get_user_basket_key(user_id: str) -> str:
    return f"basket:{user_id}"


def fetch_menu_item_details(menu_item_guid: UUID):
    try:
        response = requests.get(f"{MENU_ITEM_SERVICE_URL}/{menu_item_guid}")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as e:
        print(f"Error contacting Menu Item Service: {e}")
        return None


app = FastAPI()


app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Replace for production!
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/api/basket/{user_id}", response_model=list[MenuItem])
def get_basket(user_id: str):
    key = get_user_basket_key(user_id)
    items = redis_client.hvals(key)
    basket = []

    print(f"Fetching basket for user {user_id}: found {len(items)} item(s).")

    for item in items:
        try:
            data = json.loads(item)
            basket.append(MenuItem(**data))
        except Exception as e:
            print(f"Error parsing Redis item: {e}")

    return basket


@app.post("/api/basket/{user_id}", status_code=status.HTTP_201_CREATED)
def add_to_basket(user_id: str, menu_item: MenuItem):
    key = get_user_basket_key(user_id)
    if menu_item.price <= 0:
        raise HTTPException(
            status_code=400, detail="Menu item price must be greater than 0."
        )

    if not menu_item.menu_item_guid:
        menu_item.menu_item_guid = uuid4()

    menu_item_data = fetch_menu_item_details(menu_item.menu_item_guid)
    if not menu_item_data:
        raise HTTPException(
            status_code=404, detail="Menu item not found in Catalog Service."
        )

    print(f"Adding menu item {menu_item.name} with ID {menu_item.menu_item_guid} to {key}")
    redis_client.hset(key, str(menu_item.menu_item_guid), menu_item.model_dump_json())

    return {"message": f"Menu item '{menu_item.name}' added to basket."}


@app.put("/api/basket/{user_id}/{menu_item_guid}")
def update_basket_item(user_id: str, menu_item_guid: UUID, menu_item: MenuItem):
    key = get_user_basket_key(user_id)

    if not redis_client.hexists(key, str(menu_item_guid)):
        raise HTTPException(status_code=404, detail="Menu item not found in basket.")

    print(f"Updating menu item {menu_item.name} with ID {menu_item.menu_item_guid} in {key}")

    redis_client.hset(key, str(menu_item.menu_item_guid), menu_item.model_dump_json())

    return {"message": f"Menu item '{menu_item.name}' updated in basket."}


@app.delete("/api/basket/{user_id}/{menu_item_guid}")
def remove_from_basket(user_id: str, menu_item_guid: UUID):
    key = get_user_basket_key(user_id)

    if redis_client.hdel(key, str(menu_item_guid)) == 0:
        raise HTTPException(status_code=404, detail="Menu item not found in basket.")

    print(f"Removed menu item {menu_item_guid} from {key}")

    return {"message": f"Menu item {menu_item_guid} removed from basket."}


@app.delete("/api/basket/{user_id}")
def clear_basket(user_id: str):
    key = get_user_basket_key(user_id)
    redis_client.delete(key)
    print(f"Cleared basket for {key}")
    return {"message": "Basket cleared."}
