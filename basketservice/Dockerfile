FROM python:3.13-slim

WORKDIR /basketservice

COPY requirements.txt .

RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 8000

CMD ["uvicorn", "basket_rest_controller:app", "--host", "0.0.0.0", "--port", "8000"]