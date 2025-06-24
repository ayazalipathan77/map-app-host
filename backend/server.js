const express = require('express');
const app = express();
const PORT = process.env.PORT || 3000;
app.use(express.json());
app.get('/api/pins', (req, res) => {
    res.json([{ lat: 24.86, lng: 67.01, description: "Karachi", images: [] }]);
});
app.listen(PORT, () => console.log(`Server running on port ${PORT}`));
