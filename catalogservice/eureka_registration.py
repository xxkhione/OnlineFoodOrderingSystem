import os
import socket
import py_eureka_client.eureka_client as eureka_client

_client_inited = False

async def register_with_eureka():
    global _client_inited
    if _client_inited:
        return

    eureka_server = os.getenv("EUREKA_SERVER", "http://SEN300EurekaServer:8761/eureka/")
    app_name = os.getenv("EUREKA_APP_NAME", "catalogservice")
    instance_host = os.getenv("EUREKA_INSTANCE_HOST", socket.gethostname())
    instance_port = int(os.getenv("EUREKA_INSTANCE_PORT", "5000"))
    health_path = os.getenv("EUREKA_HEALTH_PATH", "/health")

    await eureka_client.init_async(
        eureka_server=eureka_server,
        app_name=app_name,
        instance_host=instance_host,
        instance_port=instance_port,
        health_check_url=f"http://{instance_host}:{instance_port}{health_path}",
        home_page_url=f"http://{instance_host}:{instance_port}/"
    )

    _client_inited = True