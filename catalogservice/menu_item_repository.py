# So to be honest with you, I got tired of writing this out
# So all of this code besides lines 4-13 are AI generated from Perplexity.

import os
import re
from bson import ObjectId
from pymongo import MongoClient
from menu_item import MenuItem

mongo_uri = os.getenv("MONGO_URI", "mongodb://localhost:27017/")
client = MongoClient(mongo_uri)
db = client["food_ordering_system"]
collection = db["menu_items"]


class MenuItemRepository:
    @staticmethod
    def get_all_menu_items():
        menu_items = list(collection.find({}, {"_id": 0}))
        return [MenuItem(**item).model_dump(mode="json") for item in menu_items]

    @staticmethod
    def get_menu_item_by_id(menu_item_guid):
        menu_item = collection.find_one({"menu_item_guid": str(menu_item_guid)}, {"_id": 0})
        return MenuItem(**menu_item).model_dump(mode="json") if menu_item else None

    @staticmethod
    def add_menu_item(menu_item_data):
        menu_item = MenuItem(**menu_item_data)

        menu_item_dict = menu_item.model_dump()
        menu_item_dict["menu_item_guid"] = str(menu_item_dict["menu_item_guid"])

        result = collection.insert_one(menu_item_dict)
        menu_item_dict.pop("_id", None)
        return menu_item_dict

    @staticmethod
    def update_menu_item(menu_item_guid, menu_item_data):
        existing_item = collection.find_one({"menu_item_guid": str(menu_item_guid)})
        if not existing_item:
            return None

        updated_data = {
            "menu_item_guid": existing_item["menu_item_guid"],
            "name": menu_item_data.get("name", existing_item["name"]),
            "menu_type": menu_item_data.get("menu_type", existing_item["menu_type"]),
            "description": menu_item_data.get("description", existing_item["description"]),
            "price": menu_item_data.get("price", existing_item["price"]),
        }

        validated_item = MenuItem(**updated_data)
        validated_item_dict = validated_item.model_dump()
        validated_item_dict["menu_item_guid"] = str(validated_item_dict["menu_item_guid"])

        result = collection.update_one(
            {"menu_item_guid": str(menu_item_guid)},
            {"$set": validated_item_dict}
        )
        return str(menu_item_guid) if result.matched_count > 0 else None

    @staticmethod
    def delete_menu_item(menu_item_guid):
        result = collection.delete_one({"menu_item_guid": str(menu_item_guid)})
        return str(menu_item_guid) if result.deleted_count > 0 else None

    @staticmethod
    def search_menu_items(search_text):
        escaped_text = re.escape(search_text)
        search_results = list(
            collection.find({
                "$or": [
                    {"name": {"$regex": escaped_text, "$options": "i"}},
                    {"description": {"$regex": escaped_text, "$options": "i"}},
                ]
            }, {"_id": 0}
            )
        )
        return [MenuItem(**item).model_dump(mode="json") for item in search_results]
