from pydantic import BaseModel, Field
from uuid import uuid4, UUID


class MenuItem(BaseModel):
    menu_item_guid: UUID = Field(default_factory=uuid4)
    name: str
    menu_type: str
    description: str
    price: float