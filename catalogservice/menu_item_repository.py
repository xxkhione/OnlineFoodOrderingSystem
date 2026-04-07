from pymongo import MongoClient

client = MongoClient("mongodb://localhost:27017/")
db = client["food_ordering_system"]
collection = db["menu_items"]


# Interact with Mongo here, probably going to be a class with static methods
class MenuItemRepository:
    @staticmethod
    def get_all_menu_items():
        # Placeholder for MongoDB interaction to fetch all menu items
        return []

    @staticmethod
    def get_menu_item_by_id(menu_item_id):
        # Placeholder for MongoDB interaction to fetch a menu item by ID
        return None

    @staticmethod
    def add_menu_item(menu_item_data):
        # Placeholder for MongoDB interaction to add a new menu item
        return None

    @staticmethod
    def update_menu_item(menu_item_id, menu_item_data):
        # Placeholder for MongoDB interaction to update an existing menu item
        return None

    @staticmethod
    def delete_menu_item(menu_item_id):
        # Placeholder for MongoDB interaction to delete a menu item by ID
        return None
