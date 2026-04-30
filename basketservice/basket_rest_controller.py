from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv
from uuid import UUID, uuid4
from contextlib import asynccontextmanager
import os
import redis
import json
import requests
from menu_item import MenuItem
from eureka_registration import register_with_eureka

load_dotenv()

REDIS_HOST = os.getenv("REDIS_HOST", "localhost")
REDIS_PORT = int(os.getenv("REDIS_PORT", 6379))
MENU_ITEM_SERVICE_URL = os.getenv("MENU_ITEM_SERVICE_URL", "http://catalog_service:5000/api/menu-items")
CUSTOMER_SERVICE_URL = os.getenv("CUSTOMER_SERVICE_URL", "http://customer_service:8002/api/customers")


redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, db=0, decode_responses=True)


def get_customer_basket_key(customer_id: str) -> str:
    return f"basket:{customer_id}"


def fetch_menu_item_details(menu_item_guid: UUID):
    try:
        response = requests.get(f"{MENU_ITEM_SERVICE_URL}/{menu_item_guid}")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as e:
        print(f"Error contacting Menu Item Service: {e}")
        return None


@asynccontextmanager
async def lifespan(app: FastAPI):
    await register_with_eureka()
    yield


app = FastAPI(lifespan=lifespan)


@app.get("/health")
def health():
    return {"status": "ok"}


app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/api/basket/test")
def test_basket():
    return {"message": "Basket Service is up and running!"}


@app.get("/api/basket/{customer_id}", response_model=list[MenuItem])
def get_basket(customer_id: str):
    key = get_customer_basket_key(customer_id)
    items = redis_client.hvals(key)
    basket = []

    print(f"Fetching basket for customer {customer_id}: found {len(items)} item(s).")

    for item in items:
        try:
            data = json.loads(item)
            menu_item = MenuItem(**data)
            basket.append(menu_item.model_copy(update={"customer_id": UUID(customer_id)}))
        except Exception as e:
            print(f"Error parsing Redis item: {e}")

    return basket


@app.post("/api/basket/{customer_id}", status_code=status.HTTP_201_CREATED)
def add_to_basket(customer_id: str, menu_item: MenuItem):
    key = get_customer_basket_key(customer_id)
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


@app.put("/api/basket/{customer_id}/{menu_item_guid}")
def update_basket_item(customer_id: str, menu_item_guid: UUID, menu_item: MenuItem):
    key = get_customer_basket_key(customer_id)

    if not redis_client.hexists(key, str(menu_item_guid)):
        raise HTTPException(status_code=404, detail="Menu item not found in basket.")

    print(f"Updating menu item {menu_item.name} with ID {menu_item.menu_item_guid} in {key}")

    redis_client.hset(key, str(menu_item.menu_item_guid), menu_item.model_dump_json())

    return {"message": f"Menu item '{menu_item.name}' updated in basket."}


@app.delete("/api/basket/{customer_id}/{menu_item_guid}")
def remove_from_basket(customer_id: str, menu_item_guid: UUID):
    key = get_customer_basket_key(customer_id)

    if redis_client.hdel(key, str(menu_item_guid)) == 0:
        raise HTTPException(status_code=404, detail="Menu item not found in basket.")

    print(f"Removed menu item {menu_item_guid} from {key}")

    return {"message": f"Menu item {menu_item_guid} removed from basket."}


@app.delete("/api/basket/{customer_id}")
def clear_basket(customer_id: str):
    key = get_customer_basket_key(customer_id)
    redis_client.delete(key)
    print(f"Cleared basket for {key}")
    return {"message": "Basket cleared."}
