from .clara.router import router as clara_router
from .common.router import router as common_router
from .lea.router import router as lea_router

routers = [lea_router, clara_router, common_router]
