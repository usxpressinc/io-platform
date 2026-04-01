// MongoDB initialization script
db = db.getSiblingDB('io-platform');

// Create collections and indexes
db.createCollection('users');
db.createCollection('email_logs');
db.createCollection('rate_cards');
db.createCollection('price_quotes');
db.createCollection('vendors');
db.createCollection('vendor_performance');
db.createCollection('vendor_compliance');
db.createCollection('jobs');
db.createCollection('carriers');

// Create indexes for better performance
db.users.createIndex({ "id": 1 }, { unique: true });
db.users.createIndex({ "email": 1 }, { unique: true });
db.users.createIndex({ "lastLoginAt": 1 });

db.email_logs.createIndex({ "id": 1 }, { unique: true });
db.email_logs.createIndex({ "toEmail": 1 });
db.email_logs.createIndex({ "status": 1 });
db.email_logs.createIndex({ "sentAt": 1 });

db.rate_cards.createIndex({ "id": 1 }, { unique: true });
db.rate_cards.createIndex({ "serviceType": 1 });
db.rate_cards.createIndex({ "isActive": 1 });

db.price_quotes.createIndex({ "id": 1 }, { unique: true });
db.price_quotes.createIndex({ "customerId": 1 });
db.price_quotes.createIndex({ "serviceType": 1 });
db.price_quotes.createIndex({ "createdAt": 1 });

db.vendors.createIndex({ "id": 1 }, { unique: true });
db.vendors.createIndex({ "vendorCode": 1 }, { unique: true });
db.vendors.createIndex({ "status": 1 });
db.vendors.createIndex({ "services": 1 });

db.vendor_performance.createIndex({ "id": 1 }, { unique: true });
db.vendor_performance.createIndex({ "vendorId": 1 });
db.vendor_performance.createIndex({ "periodEnd": 1 });

db.vendor_compliance.createIndex({ "id": 1 }, { unique: true });
db.vendor_compliance.createIndex({ "vendorId": 1 }, { unique: true });

db.jobs.createIndex({ "id": 1 }, { unique: true });
db.jobs.createIndex({ "customerId": 1 });
db.jobs.createIndex({ "status": 1 });
db.jobs.createIndex({ "googleJobId": 1 });

db.carriers.createIndex({ "id": 1 }, { unique: true });
db.carriers.createIndex({ "dotNumber": 1 }, { unique: true });
db.carriers.createIndex({ "mcNumber": 1 });
db.carriers.createIndex({ "vettingStatus": 1 });

// Insert some sample data for testing
db.users.insertOne({
  "id": "user-123",
  "email": "demo@usxpress.com",
  "firstName": "Demo",
  "lastName": "User",
  "role": "Admin",
  "preferences": {},
  "lastLoginAt": new Date(),
  "createdAt": new Date(),
  "updatedAt": new Date(),
  "metadata": {}
});

db.vendors.insertOne({
  "id": "vendor-123",
  "vendorCode": "DEMO-001",
  "name": "Demo Vendor Services",
  "description": "A demo vendor for testing",
  "address": "123 Demo St",
  "city": "Demo City",
  "state": "TX",
  "zipCode": "75001",
  "contactName": "John Demo",
  "contactEmail": "john@demovendor.com",
  "contactPhone": "555-0123",
  "services": ["transportation", "logistics"],
  "status": "Active",
  "rating": 4.5,
  "reviewCount": 10,
  "createdAt": new Date(),
  "updatedAt": new Date(),
  "metadata": {}
});

db.rate_cards.insertOne({
  "id": "rate-123",
  "name": "Demo Rate Card",
  "description": "Demo rate card for testing",
  "serviceType": "transportation",
  "perMileRate": 2.50,
  "perPoundRate": 0.10,
  "perCubicFootRate": 0.25,
  "flatRate": 100.00,
  "minimumCharge": 50.00,
  "fuelSurchargePercent": 5.0,
  "isActive": true,
  "priority": 1,
  "createdAt": new Date(),
  "updatedAt": new Date(),
  "metadata": {}
});

print("MongoDB initialization completed successfully!");
