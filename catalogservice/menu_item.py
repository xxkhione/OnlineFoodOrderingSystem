from pydantic import BaseModel
from uuid import uuid1


class MenuItem(BaseModel):
    menu_item_guid: uuid1
    name: str
    menu_type: str
    description: str
    price: float
