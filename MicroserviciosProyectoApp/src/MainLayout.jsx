import Navbar from 'react-bootstrap/Navbar';
import Nav from 'react-bootstrap/Nav';
import NavItem from 'react-bootstrap/NavItem';
import Button from 'react-bootstrap/Button';
import { Outlet } from "react-router";
import Container from 'react-bootstrap/Container';

function MainLayout() {
  function handleLogout() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  return (
    <>
      <Navbar expand="lg" className="bg-body-tertiary">
        <Nav className="ms-auto" navbar>
          <NavItem className="d-flex align-items-center">
            <Button color="danger" size="sm" className="mx-3" onClick={handleLogout}>
              Cerrar sesión
            </Button>
          </NavItem>
        </Nav>
      </Navbar>
      <Container>
        <Outlet />
      </Container>
    </>
  );
}

export default MainLayout;