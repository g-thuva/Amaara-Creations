import React from "react";
import { Link } from "react-router-dom";
import HeroSlider from "../components/HeroSlider";
import "./Home.css";

const Home = () => {
  return (
    <div className="home-page">
      {/* Hero Slider */}
      <HeroSlider />
      
      {/* Featured Categories */}
      <section className="section">
        <div className="container">
          <h2 className="section-title">Our Popular Categories</h2>
          <div className="category-grid">
            {[
              {
                title: 'Wedding Stickers',
                description: 'Elegant designs for your special day',
                image: 'https://images.unsplash.com/photo-1519225421980-715cb0215aed?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=60',
                link: '/products?category=wedding'
              },
              {
                title: 'Car Decals',
                description: 'Durable stickers for your vehicle',
                image: 'https://images.unsplash.com/photo-1555215695-3004980ad54e?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=60',
                link: '/products?category=car'
              },
              {
                title: 'Custom Designs',
                description: 'Bring your ideas to life',
                image: 'https://images.unsplash.com/photo-1581093196270-5a23b799d8e9?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=60',
                link: '/custom'
              }
            ].map((category, index) => (
              <div key={index} className="category-card">
                <div 
                  className="category-image"
                  style={{ backgroundImage: `url(${category.image})` }}
                >
                  <div className="category-overlay"></div>
                </div>
                <div className="category-content">
                  <h3>{category.title}</h3>
                  <p>{category.description}</p>
                  <Link to={category.link} className="btn btn-outline">
                    Shop Now
                  </Link>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>
      
      {/* Call to Action */}
      <section className="cta-section">
        <div className="container">
          <div className="cta-content">
            <h2>Ready to Create Something Amazing?</h2>
            <p>Design your custom stickers today with our easy-to-use online designer</p>
            <Link to="/custom" className="btn btn-primary btn-lg">
              Start Designing
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
};

export default Home;
