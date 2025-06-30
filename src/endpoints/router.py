from .cass.router import router as cass_router
from .common.router import router as common_router
from .elsa.router import router as elsa_router
from .larry.router import router as larry_router
from .lea.router import router as lea_router

routers = [lea_router, cass_router, common_router, elsa_router, larry_router]
