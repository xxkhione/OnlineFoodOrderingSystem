from pydantic import BaseModel, Field
from typing import Optional
from uuid import uuid4, UUID


class MenuItem(BaseModel):
    customer_id: Optional[UUID] = Field(default=None, description="The ID of the user associated with the menu item")
    menu_item_guid: UUID = Field(default_factory=uuid4, description="Unique identifier for the menu item")
    name: str
    menu_type: str
    description: Optional[str] = None
    price: float