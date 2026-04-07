from pydantic import BaseModel
from uuid import uuid1

class MenuItem(BaseModel):
    menuItemGuid: uuid1
    name: str
    menuType: str
    description: str
    price: float