import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import '../styles/HeroSlider.css';

const slides = [
  {
    id: 1,
    title: 'Custom Wedding Stickers',
    description: 'Make your special day unforgettable with our elegant wedding stickers',
    image: 'https://images.unsplash.com/photo-1519225421980-715cb0215aed?ixlib=rb-4.0.3&auto=format&fit=crop&w=1950&q=80',
    buttonText: 'View Collection',
    link: '/products?category=wedding'
  },
  {
    id: 2,
    title: 'Car Decals & Stickers',
    description: 'Personalize your vehicle with our high-quality, durable car stickers',
    image: 'https://images.unsplash.com/photo-1555215695-3004980ad54e?ixlib=rb-4.0.3&auto=format&fit=crop&w=1950&q=80',
    buttonText: 'Shop Now',
    link: '/products?category=car'
  },
  {
    id: 3,
    title: 'Custom Designs',
    description: 'Bring your ideas to life with our custom sticker printing service',
    image: 'https://images.unsplash.com/photo-1581093196270-5a23b799d8e9?ixlib=rb-4.0.3&auto=format&fit=crop&w=1950&q=80',
    buttonText: 'Get Started',
    link: '/custom'
  }
];

const HeroSlider = () => {
  const [currentSlide, setCurrentSlide] = useState(0);
  const [isAutoPlaying, setIsAutoPlaying] = useState(true);

  const nextSlide = () => {
    setCurrentSlide((prev) => (prev === slides.length - 1 ? 0 : prev + 1));
  };

  const prevSlide = () => {
    setCurrentSlide((prev) => (prev === 0 ? slides.length - 1 : prev - 1));
  };

  const goToSlide = (index) => {
    setCurrentSlide(index);
  };

  useEffect(() => {
    if (!isAutoPlaying) return;
    
    const interval = setInterval(() => {
      nextSlide();
    }, 5000);

    return () => clearInterval(interval);
  }, [currentSlide, isAutoPlaying]);

  return (
    <div 
      className="hero-slider"
      onMouseEnter={() => setIsAutoPlaying(false)}
      onMouseLeave={() => setIsAutoPlaying(true)}
    >
      <div className="slider-container">
        {slides.map((slide, index) => (
          <div 
            key={slide.id}
            className={`slide ${index === currentSlide ? 'active' : ''}`}
            style={{ backgroundImage: `url(${slide.image})` }}
          >
            <div className="slide-overlay"></div>
            <div className="slide-content">
              <h2>{slide.title}</h2>
              <p>{slide.description}</p>
              <Link to={slide.link} className="btn btn-primary">
                {slide.buttonText}
              </Link>
            </div>
          </div>
        ))}

        <button className="slider-nav prev" onClick={prevSlide}>
          <span className="sr-only"></span>
          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M15 18l-6-6 6-6" />
          </svg>
        </button>
        <button className="slider-nav next" onClick={nextSlide}>
          <span className="sr-only">  </span>
          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M9 18l6-6-6-6" />
          </svg>
        </button>

        <div className="slider-dots">
          {slides.map((_, index) => (
            <button
              key={index}
              className={`dot ${index === currentSlide ? 'active' : ''}`}
              onClick={() => goToSlide(index)}
              aria-label={`Go to slide ${index + 1}`}
            />
          ))}
        </div>
      </div>
    </div>
  );
};

export default HeroSlider;
