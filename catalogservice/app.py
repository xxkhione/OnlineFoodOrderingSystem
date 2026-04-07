from flask import Flask, request, jsonify

app = Flask(__name__)

@app.get("/api/menu-items/test")
def test():
    return jsonify({"message": "Menu Item Service is up and running!"})

@app.get("/api/menu-items")
def get_menu_items():
    # Placeholder for fetching menu items from the database
    menu_items = [
        {"id": 1, "name": "Pizza", "description": "Delicious cheese pizza", "price": 9.99},
        {"id": 2, "name": "Burger", "description": "Juicy beef burger", "price": 7.99},
    ]
    return jsonify(menu_items)

@app.post("/api/menu-items")
def add_menu_item():
    # Placeholder for adding a new menu item to the database
    return jsonify({"message": "Menu item added successfully!"})

@app.put("/api/menu-items/<int:menu_item_guid>")
def update_menu_item(menu_item_guid):
    # Placeholder for updating an existing menu item in the database
    return jsonify({"message": f"Menu item with ID {menu_item_guid} updated successfully!"})

@app.delete("/api/menu-items/<int:menu_item_guid>")
def delete_menu_item(menu_item_guid):
    # Placeholder for deleting a menu item from the database
    return jsonify({"message": f"Menu item with ID {menu_item_guid} deleted successfully!"})

@app.get("/api/menu-items/search/<str:search_text>")
def search_menu_items(search_text):
    # Placeholder for searching menu items by title or description in the database
    return jsonify({"message": f"Search results for '{search_text}'!"})
