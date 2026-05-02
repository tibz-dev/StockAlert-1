import axios from 'axios';

const api = axios.create({
  baseURL: 'https://localhost:7035/api', 
});

// Automatically attach the JWT token if we have one
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;