import asyncio
from flask import Flask, request, jsonify
from menu_item_repository import MenuItemRepository
from eureka_registration import register_with_eureka

app = Flask(__name__)
app.json.sort_keys = False


asyncio.run(register_with_eureka())


@app.get("/health")
def health():
    return {"status": "ok"}, 200


@app.get("/api/menu-items/test")
def test():
    return jsonify({"message": "Catalog Service is up and running!"})


@app.get("/api/menu-items")
def get_menu_items():
    return jsonify(MenuItemRepository.get_all_menu_items())


@app.get("/api/menu-items/<menu_item_id>")
def get_menu_item(menu_item_id):
    menu_item = MenuItemRepository.get_menu_item_by_id(menu_item_id)
    return jsonify(menu_item if menu_item else {"error": "Menu item not found"}), (200 if menu_item else 404)


@app.post("/api/menu-items")
def add_menu_item():
    new_menu_item_data = request.get_json()

    if not new_menu_item_data:
        return jsonify({"error": "Invalid input data"}), 400

    new_menu_item = MenuItemRepository.add_menu_item(new_menu_item_data)
    return (jsonify({"message": "Menu item added successfully!", "menu_item": new_menu_item}), 201)


@app.put("/api/menu-items/<menu_item_guid>")
def update_menu_item(menu_item_guid):
    updated_menu_item_data = request.get_json()

    if not updated_menu_item_data:
        return jsonify({"error": "Invalid input data"}), 400

    updated_menu_item = MenuItemRepository.update_menu_item(menu_item_guid, updated_menu_item_data)
    if not updated_menu_item:
        return jsonify({"error": "Menu item not found"}), 404

    return jsonify({"message": f"Menu item with ID {updated_menu_item} updated successfully!"})


@app.delete("/api/menu-items/<menu_item_guid>")
def delete_menu_item(menu_item_guid):
    deleted_menu_item = MenuItemRepository.delete_menu_item(menu_item_guid)
    
    if not deleted_menu_item:
        return jsonify({"error": "Menu item not found"}), 404

    return jsonify(
        {"message": f"Menu item with ID {deleted_menu_item} deleted successfully!"}
    )


@app.get("/api/menu-items/search/<search_text>")
def search_menu_items(search_text):
    search_results = MenuItemRepository.search_menu_items(search_text)

    return jsonify(search_results), 200