from pydantic import BaseModel, Field, Optional
from uuid import uuid4


class MenuItem(BaseModel):
    user_id: Optional[str] = Field(default=None, description="The ID of the user associated with the menu item") #Just until I get the auth/user service up and running
    menu_item_guid: uuid4
    name: str
    menu_type: str
    description: Optional[str] = None
    price: float